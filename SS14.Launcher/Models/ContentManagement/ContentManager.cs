using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Serilog;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Models.ContentManagement;

public sealed class ContentManager
{
    public void Initialize()
    {
        using var con = GetSqliteConnection();

        // Enable WAL journal mode so background downloads do not lock SQLite when game client is active.
        con.Execute("PRAGMA journal_mode=WAL");

        Log.Debug("Migrating content database...");

        var sw = Stopwatch.StartNew();
        var success = Migrator.Migrate(con, "SS14.Launcher.Models.ContentManagement.Migrations");
        if (!success)
            throw new Exception("Migrations failed!");

        Log.Debug("Did content DB migrations in {MigrationTime}", sw.Elapsed);
    }

    /// <summary>
    /// Clear ALL installed server content and try to truncate the DB.
    /// </summary>
    /// <returns><see langword="false"/> if a client is running and blocking the purge.</returns>
    public async Task<bool> ClearAll()
    {
        return await Task.Run(() =>
        {
            try
            {
                using var con = GetSqliteConnection();

                using var transact = con.BeginTransaction(deferred: true);

                if (GetRunningClientVersions(con).Count > 0)
                {
                    // In case GetRunningClientVersions cleaned anything up.
                    transact.Commit();
                    return false;
                }

                con.Execute("DELETE FROM InterruptedDownload");
                con.Execute("DELETE FROM ContentVersion");
                con.Execute("DELETE FROM Content");
                transact.Commit();

                con.Execute("VACUUM");
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while truncating content DB!");
                return true;
            }
        });
    }

    /// <summary>
    /// Open a blob in a manifest version for reading.
    /// </summary>
    /// <returns>null if the file does not exist.</returns>
    public static Stream? OpenBlob(SqliteConnection con, long versionId, string fileName)
    {
        var (manifestRowId, manifestCompression) = con.QueryFirstOrDefault<(long id, ContentCompressionScheme compression)>(
            @"SELECT c.ROWID, c.Compression FROM ContentManifest cm, Content c
            WHERE Path = @FileName AND VersionId = @Version AND c.Id = cm.ContentId",
            new
            {
                Version = versionId,
                FileName = fileName
            });

        if (manifestRowId == 0)
            return null;

        var blob = new SqliteBlob(con, "Content", "Data", manifestRowId, readOnly: true);

        switch (manifestCompression)
        {
            case ContentCompressionScheme.None:
                return blob;

            case ContentCompressionScheme.Deflate:
                return new DeflateStream(blob, CompressionMode.Decompress);

            case ContentCompressionScheme.ZStd:
                return new ZStdDecompressStream(blob);

            default:
                throw new InvalidDataException("Unknown compression scheme in ContentDB!");
        }
    }

    public static SqliteConnection GetSqliteConnection()
    {
        var con = new SqliteConnection(GetContentDbConnectionString());
        con.Open();
        con.Execute("PRAGMA journal_mode=WAL;");
        con.Execute("PRAGMA synchronous=NORMAL;");
        con.Execute("PRAGMA busy_timeout=30000;");
        return con;
    }

    private static string GetContentDbConnectionString()
    {
        // Disable pooling: interactions with the content DB are relatively infrequent
        // This means that ALL connections get closed in most cases (between committing download and starting client)
        // Which in turn means that the WAL file gets truncated.
        // The WAL file can get quite large in some cases (100+ MB),
        // especially as some codebases keep growing,
        // so not keeping it around for any longer than necessary is good in my book.
        //
        // (also it means that hitting the "clear server content" button in settings IMMEDIATELY truncates the DB file
        // instead of waiting for the launcher to exit, at least if the client isn't running so it can checkpoint)
        return $"Data Source={LauncherPaths.PathContentDb};Mode=ReadWriteCreate;Default Timeout=30;Pooling=False;Foreign Keys=True";
    }

    /// <summary>
    /// Get a list of content version IDs that are in use by running clients.
    /// </summary>
    /// <remarks>
    /// This method may make modifications to the DB to remove orphaned <c>RunningClient</c> entries.
    /// </remarks>
    internal static List<long> GetRunningClientVersions(SqliteConnection con)
    {
        var running = new List<long>();
        var toRemove = new List<int>();

        var dbRunning = con.Query<(int pid, string mainModule, long usedVersion)>("SELECT ProcessId, MainModule, UsedVersion FROM RunningClient");

        foreach (var (pid, mainModule, usedVersion) in dbRunning)
        {
            if (IsProcessStillRunning(pid, mainModule))
            {
                running.Add(usedVersion);
            }
            else
            {
                toRemove.Add(pid);
            }
        }

        foreach (var pid in toRemove)
        {
            con.Execute("DELETE FROM RunningClient WHERE ProcessId = @pid", new { pid });
        }

        return running;
    }

    private static bool IsProcessStillRunning(int pid, string mainModule)
    {
        try
        {
            var proc = Process.GetProcessById(pid);
            if (proc.HasExited)
                return false;

            try
            {
                return proc.MainModule?.FileName == mainModule;
            }
            catch (Exception ex)
            {
                return !proc.HasExited;
            }
        }
        catch (Exception ex)
        {
            // Process doesn't exist or access denied.
            return false;
        }
    }

    public async Task<int> RunSmartCleanerAsync(int maxAgeDays = 14)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var con = GetSqliteConnection();
                using var transact = con.BeginTransaction(deferred: true);

                var running = GetRunningClientVersions(con);
                var cutoff = DateTime.UtcNow - TimeSpan.FromDays(maxAgeDays);

                var oldVersions = con.Query<long>(
                    "SELECT Id FROM ContentVersion WHERE LastUsed < @Cutoff",
                    new { Cutoff = cutoff }).Where(id => !running.Contains(id)).ToList();

                int culled = 0;
                foreach (var vid in oldVersions)
                {
                    con.Execute("DELETE FROM ContentManifest WHERE VersionId = @VersionId", new { VersionId = vid });
                    con.Execute("DELETE FROM ContentVersion WHERE Id = @Id", new { Id = vid });
                    culled++;
                }

                con.Execute("DELETE FROM Content WHERE Id NOT IN (SELECT DISTINCT ContentId FROM ContentManifest)");
                con.Execute("DELETE FROM InterruptedDownload WHERE Added < @Cutoff", new { Cutoff = cutoff });

                transact.Commit();
                con.Execute("PRAGMA optimize;");

                Log.Information("Smart Cache Cleaner culled {CulledVersions} old content versions", culled);
                return culled;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to run Smart Cache Cleaner");
                return 0;
            }
        });
    }

    public async Task<(bool IntegrityOk, int CleanedOrphans)> VerifyAndOptimizeDatabaseAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                using var con = GetSqliteConnection();
                var integrityResult = con.QueryFirstOrDefault<string>("PRAGMA integrity_check") ?? "unknown";
                var isOk = string.Equals(integrityResult, "ok", StringComparison.OrdinalIgnoreCase);

                if (!isOk)
                {
                    Log.Warning("Content DB PRAGMA integrity_check reported: {Result}", integrityResult);
                }

                using var transact = con.BeginTransaction(deferred: true);
                var cleaned = con.Execute("DELETE FROM Content WHERE Id NOT IN (SELECT DISTINCT ContentId FROM ContentManifest)");
                con.Execute("DELETE FROM InterruptedDownload WHERE Added < DATETIME('now', '-7 days')");
                transact.Commit();

                con.Execute("PRAGMA optimize; PRAGMA wal_checkpoint(TRUNCATE);");

                Log.Information("Content DB optimized, integrity: {Integrity}, cleaned orphans: {Cleaned}", isOk, cleaned);
                return (isOk, cleaned);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to verify/optimize content DB");
                return (false, 0);
            }
        });
    }
}
