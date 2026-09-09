using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SS14.Launcher.Models.ContentManagement;

/// <summary>
/// Represents a cached record containing metadata and user customizations for a single SS14 replay archive.
/// </summary>
public sealed class ReplayMetadataEntry
{
    /// <summary>Absolute filesystem path to the replay zip file.</summary>
    public string FilePath { get; set; } = "";

    /// <summary>Filename including extension.</summary>
    public string FileName { get; set; } = "";

    /// <summary>Display title (typically filename without extension).</summary>
    public string Title { get; set; } = "";

    /// <summary>Size of the zip file on disk in bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>Last modification timestamp of the file on disk.</summary>
    public DateTime LastWriteTime { get; set; }

    /// <summary>Map name played during the recorded round.</summary>
    public string MapName { get; set; } = "";

    /// <summary>Name of the server where the round took place.</summary>
    public string ServerName { get; set; } = "";

    /// <summary>Round duration information string.</summary>
    public string Duration { get; set; } = "";

    /// <summary>Gamemode (e.g. Traitor, Secret, NukeOps, Extended).</summary>
    public string Gamemode { get; set; } = "";

    /// <summary>Sequential round identifier number.</summary>
    public string RoundId { get; set; } = "";

    /// <summary>Number of files contained in the zip archive.</summary>
    public int EntriesCount { get; set; }

    /// <summary>Sum of uncompressed sizes of all archive entries in bytes.</summary>
    public long UncompressedBytes { get; set; }

    /// <summary>Whether the zip archive was successfully opened and parsed without corruption.</summary>
    public bool IsValid { get; set; } = true;

    /// <summary>Whether this replay is marked as a user favorite (pinned to top).</summary>
    public bool IsFavorite { get; set; }

    /// <summary>Optional user notes, impressions, or tags for this round.</summary>
    public string? Note { get; set; }

    /// <summary>Lazily calculated and cached cryptographic SHA-256 hash of the archive.</summary>
    public string? Sha256 { get; set; }
}

/// <summary>
/// High-performance cache for SS14 replay archives with persistent JSON backing,
/// fast change detection (mtime + size), thread-safe atomic file writing,
/// and lazy SHA-256 hash/metadata extraction.
/// </summary>
public sealed class ReplayMetadataCache
{
    private static readonly Lazy<ReplayMetadataCache> _defaultInstance = new(() => new ReplayMetadataCache());

    /// <summary>Default singleton instance registered in the application container.</summary>
    public static ReplayMetadataCache Instance => _defaultInstance.Value;

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly string _cacheFilePath;
    private readonly object _lock = new();
    private readonly Dictionary<string, ReplayMetadataEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _concurrencyLimit = new(4, 4);
    private readonly SemaphoreSlim _saveSemaphore = new(1, 1);
    private bool _isDirty;

    /// <summary>
    /// Initializes a new instance with the default user data storage path.
    /// </summary>
    public ReplayMetadataCache() : this(Path.Combine(LauncherPaths.DirUserData, "replays_cache.json"))
    {
    }

    /// <summary>
    /// Initializes a new instance with a custom cache file path (used for tests or isolated profiles).
    /// </summary>
    /// <param name="cacheFilePath">Path to the JSON cache file.</param>
    public ReplayMetadataCache(string cacheFilePath)
    {
        _cacheFilePath = cacheFilePath;
        Load();
    }

    /// <summary>
    /// Returns a snapshot dictionary of all currently cached metadata entries.
    /// </summary>
    public IReadOnlyDictionary<string, ReplayMetadataEntry> GetAllCached()
    {
        lock (_lock)
        {
            return new Dictionary<string, ReplayMetadataEntry>(_cache, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Attempts to retrieve a cached entry without reading disk if available.
    /// </summary>
    public bool TryGet(string filePath, out ReplayMetadataEntry? entry)
    {
        lock (_lock)
        {
            return _cache.TryGetValue(filePath, out entry);
        }
    }

    /// <summary>
    /// Retrieves replay metadata from cache if valid, or extracts it from the archive if missing/modified.
    /// Uses semaphore throttling to prevent UI freezes during batch indexing.
    /// </summary>
    public async Task<ReplayMetadataEntry> GetOrUpdateAsync(string filePath, CancellationToken cancel = default)
    {
        var fi = new FileInfo(filePath);
        if (!fi.Exists)
        {
            Remove(filePath);
            return new ReplayMetadataEntry
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Title = Path.GetFileNameWithoutExtension(filePath),
                IsValid = false
            };
        }

        ReplayMetadataEntry? cached;
        lock (_lock)
        {
            if (_cache.TryGetValue(filePath, out cached) &&
                cached.FileSize == fi.Length &&
                cached.LastWriteTime == fi.LastWriteTime)
            {
                return cached;
            }
        }

        await _concurrencyLimit.WaitAsync(cancel);
        try
        {
            // Re-check cache after semaphore
            lock (_lock)
            {
                if (_cache.TryGetValue(filePath, out cached) &&
                    cached.FileSize == fi.Length &&
                    cached.LastWriteTime == fi.LastWriteTime)
                {
                    return cached;
                }
            }

            var entry = await Task.Run(() => ExtractMetadata(fi, cached), cancel);

            lock (_lock)
            {
                _cache[filePath] = entry;
                _isDirty = true;
            }

            _ = SaveAsync();
            return entry;
        }
        finally
        {
            _concurrencyLimit.Release();
        }
    }

    /// <summary>
    /// Streams and parses metadata from the zip archive line-by-line to avoid large heap allocations.
    /// </summary>
    private static ReplayMetadataEntry ExtractMetadata(FileInfo fi, ReplayMetadataEntry? previous)
    {
        var entry = new ReplayMetadataEntry
        {
            FilePath = fi.FullName,
            FileName = fi.Name,
            Title = Path.GetFileNameWithoutExtension(fi.Name),
            FileSize = fi.Length,
            LastWriteTime = fi.LastWriteTime,
            IsFavorite = previous?.IsFavorite ?? false,
            Note = previous?.Note
        };

        try
        {
            using var fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64 * 1024);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Read, true);

            entry.EntriesCount = archive.Entries.Count;
            long uncompressedBytes = 0;
            foreach (var zipEntry in archive.Entries)
            {
                uncompressedBytes += zipEntry.Length;
            }
            entry.UncompressedBytes = uncompressedBytes;

            var metaEntry = archive.GetEntry("replay_final.txt")
                         ?? archive.GetEntry("replay_final.yml")
                         ?? archive.GetEntry("manifest.yml");

            if (metaEntry != null)
            {
                using var stream = metaEntry.Open();
                using var reader = new StreamReader(stream);
                string? line;

                // Stream line-by-line without allocating large arrays
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("map:", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("map_name:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = trimmed.Split(':', 2);
                        if (parts.Length > 1) entry.MapName = parts[1].Trim(' ', '"', '\'');
                    }
                    else if (trimmed.StartsWith("server:", StringComparison.OrdinalIgnoreCase) ||
                             trimmed.StartsWith("server_name:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = trimmed.Split(':', 2);
                        if (parts.Length > 1) entry.ServerName = parts[1].Trim(' ', '"', '\'');
                    }
                    else if (trimmed.StartsWith("duration:", StringComparison.OrdinalIgnoreCase) ||
                             trimmed.StartsWith("ticks:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = trimmed.Split(':', 2);
                        if (parts.Length > 1) entry.Duration = parts[1].Trim(' ', '"', '\'');
                    }
                    else if (trimmed.StartsWith("gamemode:", StringComparison.OrdinalIgnoreCase) ||
                             trimmed.StartsWith("mode:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = trimmed.Split(':', 2);
                        if (parts.Length > 1) entry.Gamemode = parts[1].Trim(' ', '"', '\'');
                    }
                    else if (trimmed.StartsWith("round_id:", StringComparison.OrdinalIgnoreCase) ||
                             trimmed.StartsWith("round:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = trimmed.Split(':', 2);
                        if (parts.Length > 1) entry.RoundId = parts[1].Trim(' ', '"', '\'');
                    }
                }
            }

            entry.IsValid = true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read zip archive metadata for replay {Path}", fi.FullName);
            entry.IsValid = false;
        }

        return entry;
    }

    /// <summary>
    /// Computes the cryptographic SHA-256 hash of the archive or returns the cached hash.
    /// </summary>
    public async Task<string> ComputeOrGetSha256Async(string filePath, CancellationToken cancel = default)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(filePath, out var cached) && !string.IsNullOrEmpty(cached.Sha256))
            {
                return cached.Sha256;
            }
        }

        if (!File.Exists(filePath))
            return string.Empty;

        var hash = await Task.Run(() =>
        {
            using var sha = SHA256.Create();
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 128 * 1024);
            var bytes = sha.ComputeHash(fs);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }, cancel);

        lock (_lock)
        {
            if (_cache.TryGetValue(filePath, out var entry))
            {
                entry.Sha256 = hash;
                _isDirty = true;
            }
        }

        _ = SaveAsync();
        return hash;
    }

    /// <summary>
    /// Marks or unmarks a replay as user favorite and triggers asynchronous persistence.
    /// </summary>
    public void SetFavorite(string filePath, bool isFavorite)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(filePath, out var entry))
            {
                entry.IsFavorite = isFavorite;
                _isDirty = true;
            }
            else
            {
                var fi = new FileInfo(filePath);
                _cache[filePath] = new ReplayMetadataEntry
                {
                    FilePath = filePath,
                    FileName = fi.Name,
                    Title = Path.GetFileNameWithoutExtension(fi.Name),
                    FileSize = fi.Exists ? fi.Length : 0,
                    LastWriteTime = fi.Exists ? fi.LastWriteTime : DateTime.MinValue,
                    IsFavorite = isFavorite
                };
                _isDirty = true;
            }
        }

        _ = SaveAsync();
    }

    /// <summary>
    /// Sets or clears a custom note for a replay and triggers asynchronous persistence.
    /// </summary>
    public void SetNote(string filePath, string? note)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(filePath, out var entry))
            {
                entry.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
                _isDirty = true;
            }
        }

        _ = SaveAsync();
    }

    /// <summary>
    /// Removes an entry from the cache by file path.
    /// </summary>
    public void Remove(string filePath)
    {
        lock (_lock)
        {
            if (_cache.Remove(filePath))
            {
                _isDirty = true;
            }
        }

        _ = SaveAsync();
    }

    /// <summary>
    /// Prunes cached entries that no longer exist on disk.
    /// </summary>
    public void PruneMissing(IEnumerable<string> existingPaths)
    {
        var set = new HashSet<string>(existingPaths, StringComparer.OrdinalIgnoreCase);
        lock (_lock)
        {
            var toRemove = new List<string>();
            foreach (var key in _cache.Keys)
            {
                if (!set.Contains(key))
                {
                    toRemove.Add(key);
                }
            }

            foreach (var key in toRemove)
            {
                _cache.Remove(key);
                _isDirty = true;
            }
        }

        if (_isDirty)
        {
            _ = SaveAsync();
        }
    }

    /// <summary>
    /// Loads cached replay metadata entries from disk.
    /// </summary>
    public void Load()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_cacheFilePath))
                    return;

                var json = File.ReadAllText(_cacheFilePath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, ReplayMetadataEntry>>(json);
                if (loaded != null)
                {
                    _cache.Clear();
                    foreach (var (k, v) in loaded)
                    {
                        _cache[k] = v;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load replay metadata cache from {Path}", _cacheFilePath);
            }
        }
    }

    /// <summary>
    /// Asynchronously flushes all dirty cache entries to disk using an atomic move operation.
    /// Protected by a semaphore to ensure thread-safe non-colliding writes.
    /// </summary>
    public async Task SaveAsync()
    {
        await _saveSemaphore.WaitAsync();
        try
        {
            string? json = null;
            lock (_lock)
            {
                if (!_isDirty)
                    return;

                try
                {
                    json = JsonSerializer.Serialize(_cache, _jsonOptions);
                    _isDirty = false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to serialize replay metadata cache");
                    return;
                }
            }

            if (json == null)
                return;

            await Task.Run(() =>
            {
                try
                {
                    var dir = Path.GetDirectoryName(_cacheFilePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var tempPath = $"{_cacheFilePath}.{Guid.NewGuid():N}.tmp";
                    File.WriteAllText(tempPath, json);
                    File.Move(tempPath, _cacheFilePath, true);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to write replay metadata cache to {Path}", _cacheFilePath);
                }
            });
        }
        finally
        {
            _saveSemaphore.Release();
        }
    }

    /// <summary>
    /// Parses a duration string (formatted as hh:mm:ss, mm:ss, or numeric seconds) into a TimeSpan.
    /// </summary>
    public static TimeSpan? ParseDuration(string? durationStr)
    {
        if (string.IsNullOrWhiteSpace(durationStr))
            return null;

        durationStr = durationStr.Trim();

        if (durationStr.Contains(':'))
        {
            if (TimeSpan.TryParse(durationStr, CultureInfo.InvariantCulture, out var ts))
                return ts;
        }
        else if (double.TryParse(durationStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return null;
    }

    /// <summary>
    /// Formats a TimeSpan into a human-readable duration string (e.g. '1d 4h 30m', '2h 15m 30s', or '45m 12s').
    /// </summary>
    public static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        return $"{ts.Minutes}m {ts.Seconds:D2}s";
    }
}
