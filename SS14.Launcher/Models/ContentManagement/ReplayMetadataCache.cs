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

public sealed class ReplayMetadataEntry
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Title { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime LastWriteTime { get; set; }
    public string MapName { get; set; } = "";
    public string ServerName { get; set; } = "";
    public string Duration { get; set; } = "";
    public string Gamemode { get; set; } = "";
    public string RoundId { get; set; } = "";
    public int EntriesCount { get; set; }
    public long UncompressedBytes { get; set; }
    public bool IsValid { get; set; } = true;
    public bool IsFavorite { get; set; }
    public string? Note { get; set; }
    public string? Sha256 { get; set; }
}

/// <summary>
/// High-performance cache for SS14 replay archives with persistent JSON backing,
/// change detection (mtime + size), and lazy hash/metadata extraction.
/// </summary>
public sealed class ReplayMetadataCache
{
    private static readonly Lazy<ReplayMetadataCache> _defaultInstance = new(() => new ReplayMetadataCache());
    public static ReplayMetadataCache Instance => _defaultInstance.Value;

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly string _cacheFilePath;
    private readonly object _lock = new();
    private readonly Dictionary<string, ReplayMetadataEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _concurrencyLimit = new(4, 4);
    private readonly SemaphoreSlim _saveSemaphore = new(1, 1);
    private bool _isDirty;

    public ReplayMetadataCache() : this(Path.Combine(LauncherPaths.DirUserData, "replays_cache.json"))
    {
    }

    public ReplayMetadataCache(string cacheFilePath)
    {
        _cacheFilePath = cacheFilePath;
        Load();
    }

    public IReadOnlyDictionary<string, ReplayMetadataEntry> GetAllCached()
    {
        lock (_lock)
        {
            return new Dictionary<string, ReplayMetadataEntry>(_cache, StringComparer.OrdinalIgnoreCase);
        }
    }

    public bool TryGet(string filePath, out ReplayMetadataEntry? entry)
    {
        lock (_lock)
        {
            return _cache.TryGetValue(filePath, out entry);
        }
    }

    /// <summary>
    /// Retrieves replay metadata from cache if valid, or extracts it from the archive if missing/modified.
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
                var content = reader.ReadToEnd();

                var lines = content.Split('\n');
                foreach (var line in lines)
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

    public static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        return $"{ts.Minutes}m {ts.Seconds:D2}s";
    }
}
