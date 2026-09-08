using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Serilog;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Models.ContentManagement;

public record struct EngineStorageItem(string Version, long SizeBytes, DateTime LastModified)
{
    public string FormattedSize => StorageAnalyzer.FormatBytes(SizeBytes);
    public string FormattedDate => LastModified.ToString("yyyy-MM-dd HH:mm");
}

public sealed class StorageBreakdown
{
    public long ContentDbBytes { get; set; }
    public long ContentWalBytes { get; set; }
    public long EnginesTotalBytes { get; set; }
    public long LogsTotalBytes { get; set; }
    public long ReplaysTotalBytes { get; set; }

    public long TotalBytes => ContentDbBytes + ContentWalBytes + EnginesTotalBytes + LogsTotalBytes + ReplaysTotalBytes;

    public List<EngineStorageItem> InstalledEngines { get; set; } = new();

    public string FormattedContentDb => StorageAnalyzer.FormatBytes(ContentDbBytes + ContentWalBytes);
    public string FormattedEngines => StorageAnalyzer.FormatBytes(EnginesTotalBytes);
    public string FormattedLogs => StorageAnalyzer.FormatBytes(LogsTotalBytes);
    public string FormattedReplays => StorageAnalyzer.FormatBytes(ReplaysTotalBytes);
    public string FormattedTotal => StorageAnalyzer.FormatBytes(TotalBytes);
}

public static class StorageAnalyzer
{
    public static string FormatBytes(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
            return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("F2", CultureInfo.InvariantCulture) + " GB";
        if (bytes >= 1024L * 1024)
            return (bytes / (1024.0 * 1024.0)).ToString("F1", CultureInfo.InvariantCulture) + " MB";
        if (bytes >= 1024L)
            return (bytes / 1024.0).ToString("F0", CultureInfo.InvariantCulture) + " KB";
        return $"{bytes} B";
    }

    public static async Task<StorageBreakdown> AnalyzeStorageAsync()
    {
        return await Task.Run(() =>
        {
            var breakdown = new StorageBreakdown();

            try
            {
                // 1. Content DB
                if (File.Exists(LauncherPaths.PathContentDb))
                {
                    breakdown.ContentDbBytes = new FileInfo(LauncherPaths.PathContentDb).Length;
                }

                var walPath = LauncherPaths.PathContentDb + "-wal";
                if (File.Exists(walPath))
                {
                    breakdown.ContentWalBytes = new FileInfo(walPath).Length;
                }

                // 2. Engines
                if (Directory.Exists(LauncherPaths.DirEngines))
                {
                    var dir = new DirectoryInfo(LauncherPaths.DirEngines);
                    foreach (var subDir in dir.GetDirectories())
                    {
                        var dirSize = GetDirectorySize(subDir);
                        breakdown.InstalledEngines.Add(new EngineStorageItem(subDir.Name, dirSize, subDir.LastWriteTime));
                        breakdown.EnginesTotalBytes += dirSize;
                    }

                    foreach (var file in dir.GetFiles("*.zip"))
                    {
                        var ver = Path.GetFileNameWithoutExtension(file.Name);
                        breakdown.InstalledEngines.Add(new EngineStorageItem(ver, file.Length, file.LastWriteTime));
                        breakdown.EnginesTotalBytes += file.Length;
                    }
                }

                // 3. Logs
                if (Directory.Exists(LauncherPaths.DirLogs))
                {
                    breakdown.LogsTotalBytes = GetDirectorySize(new DirectoryInfo(LauncherPaths.DirLogs));
                }

                // 4. Replays
                if (Directory.Exists(LauncherPaths.DirReplays))
                {
                    breakdown.ReplaysTotalBytes = GetDirectorySize(new DirectoryInfo(LauncherPaths.DirReplays));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to analyze storage breakdown");
            }

            return breakdown;
        });
    }

    public static long GetDirectorySize(DirectoryInfo d)
    {
        long size = 0;
        try
        {
            foreach (var file in d.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;
            }
        }
        catch (Exception)
        {
            // Ignore access errors on individual files
        }
        return size;
    }

    public static bool DeleteEngineVersion(string engineVersion)
    {
        try
        {
            if (!Directory.Exists(LauncherPaths.DirEngines))
                return false;

            var dir = Path.Combine(LauncherPaths.DirEngines, engineVersion);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
                return true;
            }

            var zip = Path.Combine(LauncherPaths.DirEngines, $"{engineVersion}.zip");
            if (File.Exists(zip))
            {
                File.Delete(zip);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete engine version {Version}", engineVersion);
            return false;
        }
    }

    public static async Task<bool> VacuumContentDatabaseAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                using var con = ContentManager.GetSqliteConnection();
                con.Execute("VACUUM;");
                con.Execute("PRAGMA wal_checkpoint(TRUNCATE);");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to vacuum content database");
                return false;
            }
        });
    }

    public static void ClearLogs()
    {
        try
        {
            if (!Directory.Exists(LauncherPaths.DirLogs))
                return;

            foreach (var file in Directory.EnumerateFiles(LauncherPaths.DirLogs, "*.*"))
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    // File is locked by current running process, skip
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to clear logs directory");
        }
    }
}
