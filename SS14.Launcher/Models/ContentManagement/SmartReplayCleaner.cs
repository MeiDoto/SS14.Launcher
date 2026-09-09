using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;

namespace SS14.Launcher.Models.ContentManagement;

/// <summary>
/// Configuration options controlling the criteria used by the smart replay cleaner.
/// </summary>
public sealed class CleanerOptions
{
    /// <summary>Delete archives whose last modified date is older than this many days (null to disable age filtering).</summary>
    public int? OlderThanDays { get; set; }

    /// <summary>Whether to identify and remove corrupted, unreadable, or truncated zip archives.</summary>
    public bool DeleteCorrupted { get; set; } = true;

    /// <summary>Maximum cumulative size in bytes to retain (oldest recordings exceeding this quota are pruned).</summary>
    public long? MaxTotalQuotaBytes { get; set; }

    /// <summary>Whether to protect user-starred favorites from automatic deletion under any circumstance.</summary>
    public bool ExcludeFavorites { get; set; } = true;
}

/// <summary>
/// Summary report returned after executing a smart cleaning operation.
/// </summary>
public sealed class CleanResult
{
    /// <summary>Total number of candidate entries targeted for cleanup.</summary>
    public int TotalScanned { get; set; }

    /// <summary>Number of files successfully removed from disk.</summary>
    public int DeletedCount { get; set; }

    /// <summary>Total amount of disk space reclaimed in bytes.</summary>
    public long FreedBytes { get; set; }

    /// <summary>List of file paths that failed deletion (e.g. due to file locks or filesystem permission errors).</summary>
    public List<string> FailedPaths { get; set; } = new();

    /// <summary>Human-readable string representation of the reclaimed storage space (e.g. '1.42 GB').</summary>
    public string FreedBytesFormatted => StorageAnalyzer.FormatBytes(FreedBytes);
}

/// <summary>
/// Provides logic for smart pruning, space estimation, and batch cleaning of SS14 replay files
/// based on age, disk quota, and archive corruption status.
/// </summary>
public static class SmartReplayCleaner
{
    /// <summary>
    /// Evaluates replay entries against the supplied options and returns a list of candidate files recommended for deletion.
    /// Does not perform any filesystem modifications.
    /// </summary>
    /// <param name="allItems">Collection of all known replay metadata entries.</param>
    /// <param name="options">Pruning criteria including age, quota, and favorite protection.</param>
    /// <returns>A deduplicated list of entries matching the cleaning criteria.</returns>
    public static List<ReplayMetadataEntry> Scan(IEnumerable<ReplayMetadataEntry> allItems, CleanerOptions options)
    {
        var itemsList = allItems.ToList();
        var candidates = new HashSet<ReplayMetadataEntry>();

        var eligible = options.ExcludeFavorites
            ? itemsList.Where(i => !i.IsFavorite).ToList()
            : itemsList;

        // 1. Corrupted archives
        if (options.DeleteCorrupted)
        {
            foreach (var item in eligible.Where(i => !i.IsValid))
            {
                candidates.Add(item);
            }
        }

        // 2. Age threshold
        if (options.OlderThanDays.HasValue && options.OlderThanDays.Value > 0)
        {
            var threshold = DateTime.Now.AddDays(-options.OlderThanDays.Value);
            foreach (var item in eligible.Where(i => i.LastWriteTime < threshold))
            {
                candidates.Add(item);
            }
        }

        // 3. Disk quota
        if (options.MaxTotalQuotaBytes.HasValue && options.MaxTotalQuotaBytes.Value > 0)
        {
            var quota = options.MaxTotalQuotaBytes.Value;
            var currentTotal = itemsList.Sum(i => i.FileSize);
            var candidatesTotal = candidates.Sum(c => c.FileSize);
            var remainingTotal = currentTotal - candidatesTotal;

            if (remainingTotal > quota)
            {
                // Order non-candidate items from oldest to newest to prune oldest first
                var remainingEligible = eligible
                    .Where(i => !candidates.Contains(i))
                    .OrderBy(i => i.LastWriteTime)
                    .ToList();

                foreach (var item in remainingEligible)
                {
                    candidates.Add(item);
                    remainingTotal -= item.FileSize;
                    if (remainingTotal <= quota)
                        break;
                }
            }
        }

        return candidates.ToList();
    }

    /// <summary>
    /// Deletes the specified replay files from the filesystem and removes their entries from the persistent metadata cache.
    /// </summary>
    /// <param name="toDelete">List of replay entries to delete.</param>
    /// <param name="cache">Metadata cache to be updated after file deletion.</param>
    /// <returns>A summary result containing freed bytes and deletion status.</returns>
    public static CleanResult Execute(IEnumerable<ReplayMetadataEntry> toDelete, ReplayMetadataCache cache)
    {
        var itemsList = toDelete.ToList();
        var result = new CleanResult
        {
            TotalScanned = itemsList.Count
        };

        foreach (var item in itemsList)
        {
            try
            {
                if (File.Exists(item.FilePath))
                {
                    File.Delete(item.FilePath);
                }

                cache.Remove(item.FilePath);
                result.DeletedCount++;
                result.FreedBytes += item.FileSize;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete replay during smart clean {Path}", item.FilePath);
                result.FailedPaths.Add(item.FilePath);
            }
        }

        return result;
    }
}
