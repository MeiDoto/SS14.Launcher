using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SS14.Launcher.Models.ContentManagement;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class ReplaySystemTests
{
    private string _tempDir = "";

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ss14_replay_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore teardown cleanup errors
        }
    }

    private string CreateSampleReplayZip(string fileName, string server, string map, string mode, string roundId, string duration)
    {
        var zipPath = Path.Combine(_tempDir, fileName);
        using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
        using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("replay_final.txt");
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.WriteLine($"server: {server}");
                writer.WriteLine($"map: {map}");
                writer.WriteLine($"gamemode: {mode}");
                writer.WriteLine($"round_id: {roundId}");
                writer.WriteLine($"duration: {duration}");
            }

            var dummyData = archive.CreateEntry("events.bin");
            using (var dataWriter = new BinaryWriter(dummyData.Open()))
            {
                dataWriter.Write(new byte[1024]);
            }
        }

        return zipPath;
    }

    [Test]
    public async Task TestExtractReplayMetadataFromZip()
    {
        var zip = CreateSampleReplayZip("round_100.zip", "Space Station 14 Official", "Saltern", "traitor", "100", "01:15:30");
        var cacheFile = Path.Combine(_tempDir, "cache.json");
        var cache = new ReplayMetadataCache(cacheFile);

        var entry = await cache.GetOrUpdateAsync(zip);

        Assert.That(entry, Is.Not.Null);
        Assert.That(entry.ServerName, Is.EqualTo("Space Station 14 Official"));
        Assert.That(entry.MapName, Is.EqualTo("Saltern"));
        Assert.That(entry.Gamemode, Is.EqualTo("traitor"));
        Assert.That(entry.RoundId, Is.EqualTo("100"));
        Assert.That(entry.Duration, Is.EqualTo("01:15:30"));
        Assert.That(entry.IsValid, Is.True);
        Assert.That(entry.EntriesCount, Is.EqualTo(2));
        Assert.That(entry.UncompressedBytes, Is.GreaterThan(1000));
    }

    [Test]
    public async Task TestCachePersistenceWithFavoritesAndNotes()
    {
        var zip = CreateSampleReplayZip("round_200.zip", "Sector 7", "Box", "nukeops", "200", "00:45:00");
        var cacheFile = Path.Combine(_tempDir, "cache.json");

        var cache1 = new ReplayMetadataCache(cacheFile);
        var entry1 = await cache1.GetOrUpdateAsync(zip);
        Assert.That(entry1.IsFavorite, Is.False);

        cache1.SetFavorite(zip, true);
        cache1.SetNote(zip, "Victory for the Syndicate!");
        await cache1.SaveAsync();

        // Load into a new cache instance
        var cache2 = new ReplayMetadataCache(cacheFile);
        Assert.That(cache2.TryGet(zip, out var loadedEntry), Is.True);
        Assert.That(loadedEntry!.IsFavorite, Is.True);
        Assert.That(loadedEntry.Note, Is.EqualTo("Victory for the Syndicate!"));
        Assert.That(loadedEntry.Gamemode, Is.EqualTo("nukeops"));
    }

    [Test]
    public async Task TestSha256HashCalculationAndCaching()
    {
        var zip = CreateSampleReplayZip("round_300.zip", "Main", "Delta", "secret", "300", "00:20:00");
        var cacheFile = Path.Combine(_tempDir, "cache.json");
        var cache = new ReplayMetadataCache(cacheFile);

        var hash1 = await cache.ComputeOrGetSha256Async(zip);
        Assert.That(hash1, Is.Not.Null.And.Not.Empty);
        Assert.That(hash1.Length, Is.EqualTo(64)); // SHA256 hex length

        // Second call should return the exact same hash
        var hash2 = await cache.ComputeOrGetSha256Async(zip);
        Assert.That(hash2, Is.EqualTo(hash1));
    }

    [Test]
    public void TestDurationParsingAndFormatting()
    {
        var ts1 = ReplayMetadataCache.ParseDuration("01:30:00");
        Assert.That(ts1, Is.EqualTo(TimeSpan.FromMinutes(90)));

        var ts2 = ReplayMetadataCache.ParseDuration("3600");
        Assert.That(ts2, Is.EqualTo(TimeSpan.FromHours(1)));

        var ts3 = ReplayMetadataCache.ParseDuration("invalid_time");
        Assert.That(ts3, Is.Null);

        var formattedShort = ReplayMetadataCache.FormatDuration(TimeSpan.FromMinutes(25).Add(TimeSpan.FromSeconds(14)));
        Assert.That(formattedShort, Is.EqualTo("25m 14s"));

        var formattedHours = ReplayMetadataCache.FormatDuration(TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(12)).Add(TimeSpan.FromSeconds(5)));
        Assert.That(formattedHours, Is.EqualTo("3h 12m 05s"));
    }

    [Test]
    public void TestSmartReplayCleaner_AgeFilterWithFavoritesExclusion()
    {
        var entries = new List<ReplayMetadataEntry>
        {
            new() { FilePath = "r1.zip", FileSize = 1000, LastWriteTime = DateTime.Now.AddDays(-5), IsFavorite = false, IsValid = true },
            new() { FilePath = "r2.zip", FileSize = 2000, LastWriteTime = DateTime.Now.AddDays(-20), IsFavorite = false, IsValid = true },
            new() { FilePath = "r3.zip", FileSize = 3000, LastWriteTime = DateTime.Now.AddDays(-40), IsFavorite = false, IsValid = true },
            new() { FilePath = "r4.zip", FileSize = 4000, LastWriteTime = DateTime.Now.AddDays(-50), IsFavorite = true, IsValid = true } // Favorite!
        };

        var options = new CleanerOptions
        {
            OlderThanDays = 30,
            ExcludeFavorites = true,
            DeleteCorrupted = false
        };

        var candidates = SmartReplayCleaner.Scan(entries, options);

        Assert.That(candidates.Count, Is.EqualTo(1));
        Assert.That(candidates[0].FilePath, Is.EqualTo("r3.zip"));
    }

    [Test]
    public void TestSmartReplayCleaner_QuotaFilter()
    {
        var entries = new List<ReplayMetadataEntry>
        {
            new() { FilePath = "newest.zip", FileSize = 500, LastWriteTime = DateTime.Now.AddDays(-1), IsFavorite = false, IsValid = true },
            new() { FilePath = "mid.zip", FileSize = 500, LastWriteTime = DateTime.Now.AddDays(-5), IsFavorite = false, IsValid = true },
            new() { FilePath = "oldest.zip", FileSize = 500, LastWriteTime = DateTime.Now.AddDays(-10), IsFavorite = false, IsValid = true }
        };

        // Total size = 1500. Quota = 700. We must clean at least 800 (so 2 oldest files).
        var options = new CleanerOptions
        {
            MaxTotalQuotaBytes = 700,
            ExcludeFavorites = true,
            DeleteCorrupted = false
        };

        var candidates = SmartReplayCleaner.Scan(entries, options);

        Assert.That(candidates.Count, Is.EqualTo(2));
        Assert.That(candidates.Select(c => c.FilePath), Contains.Item("oldest.zip"));
        Assert.That(candidates.Select(c => c.FilePath), Contains.Item("mid.zip"));
    }

    [Test]
    public void TestSmartReplayCleaner_CorruptedArchives()
    {
        var entries = new List<ReplayMetadataEntry>
        {
            new() { FilePath = "good.zip", FileSize = 1000, LastWriteTime = DateTime.Now, IsFavorite = false, IsValid = true },
            new() { FilePath = "broken.zip", FileSize = 500, LastWriteTime = DateTime.Now, IsFavorite = false, IsValid = false }
        };

        var options = new CleanerOptions
        {
            DeleteCorrupted = true,
            ExcludeFavorites = true
        };

        var candidates = SmartReplayCleaner.Scan(entries, options);

        Assert.That(candidates.Count, Is.EqualTo(1));
        Assert.That(candidates[0].FilePath, Is.EqualTo("broken.zip"));
    }

    [Test]
    public void TestSmartReplayCleaner_ExecutionRemovesFilesAndUpdatesCache()
    {
        var zip1 = CreateSampleReplayZip("delete_me.zip", "S", "M", "G", "1", "10:00");
        var zip2 = CreateSampleReplayZip("keep_me.zip", "S", "M", "G", "2", "10:00");

        var cacheFile = Path.Combine(_tempDir, "cache.json");
        var cache = new ReplayMetadataCache(cacheFile);

        var entry1 = new ReplayMetadataEntry { FilePath = zip1, FileSize = new FileInfo(zip1).Length, IsValid = true };
        cache.SetFavorite(zip1, false);

        var cleanResult = SmartReplayCleaner.Execute(new[] { entry1 }, cache);

        Assert.That(cleanResult.DeletedCount, Is.EqualTo(1));
        Assert.That(cleanResult.FreedBytes, Is.GreaterThan(0));
        Assert.That(File.Exists(zip1), Is.False);
        Assert.That(File.Exists(zip2), Is.True);
        Assert.That(cache.TryGet(zip1, out _), Is.False);
    }
}
