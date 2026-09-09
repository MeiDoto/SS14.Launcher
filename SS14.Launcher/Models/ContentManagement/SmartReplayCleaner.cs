using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;

namespace SS14.Launcher.Models.ContentManagement;

public sealed class CleanerOptions
{
    public int? OlderThanDays { get; set; }
    public bool DeleteCorrupted { get; set; } = true;
    public long? MaxTotalQuotaBytes { get; set; }
    public bool ExcludeFavorites { get; set; } = true;
}

public sealed class CleanResult
{
    public int TotalScanned { get; set; }
    public int DeletedCount { get; set; }
    public long FreedBytes { get; set; }
    public List<string> FailedPaths { get; set; } = new();

    public string FreedBytesFormatted => StorageAnalyzer.FormatBytes(FreedBytes);
}

/// <summary>
/// Provides logic for smart pruning and batch cleaning of SS14 replay files
/// based on age, disk quota, and archive corruption status.
/// </summary>
public static class SmartReplayCleaner
{
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
