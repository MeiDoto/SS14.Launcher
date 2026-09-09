using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SS14.Launcher.Models.ContentManagement;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class ReplayDownloaderTests
{
    [Test]
    public void NormalizeDownloadUrl_SpaceStoriesWebUrl_ConvertsToApiDownload()
    {
        var input = "https://spacestories.club/replays/core/2026_09_09-12_23-round_50135.zip";
        var expected = "https://spacestories.club/replays/api/download/core/2026_09_09-12_23-round_50135.zip";
        Assert.That(ReplayDownloader.NormalizeDownloadUrl(input), Is.EqualTo(expected));
    }

    [Test]
    public void NormalizeDownloadUrl_AlreadyApiUrl_Unchanged()
    {
        var input = "https://spacestories.club/replays/api/download/core/2026_09_09-12_23-round_50135.zip";
        Assert.That(ReplayDownloader.NormalizeDownloadUrl(input), Is.EqualTo(input));
    }

    [Test]
    public void NormalizeDownloadUrl_GenericUrl_Unchanged()
    {
        var input = "https://example.com/replays/round-123.zip";
        Assert.That(ReplayDownloader.NormalizeDownloadUrl(input), Is.EqualTo(input));
    }

    [Test]
    public async Task ResolveUrl_CustomTemplate_Success()
    {
        var url = await ReplayDownloader.ResolveUrlFromRoundIdAsync("999", ReplayProviderPreset.CustomTemplate, "https://myreplays.org/download/{roundId}.zip");
        Assert.That(url, Is.EqualTo("https://myreplays.org/download/999.zip"));
    }

    [Test]
    public void ResolveUrl_CustomTemplate_MissingPlaceholder_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await ReplayDownloader.ResolveUrlFromRoundIdAsync("999", ReplayProviderPreset.CustomTemplate, "https://myreplays.org/download/archive.zip"));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("#")]
    [TestCase("###")]
    public void ResolveUrl_InvalidRoundId_ThrowsArgumentException(string invalidId)
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await ReplayDownloader.ResolveUrlFromRoundIdAsync(invalidId, ReplayProviderPreset.SpaceStories));
    }

    [Test]
    public void ResolveUrl_CustomTemplate_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await ReplayDownloader.ResolveUrlFromRoundIdAsync("100", ReplayProviderPreset.CustomTemplate, null));

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await ReplayDownloader.ResolveUrlFromRoundIdAsync("100", ReplayProviderPreset.CustomTemplate, ""));
    }

    [TestCase("not-a-valid-url")]
    [TestCase("ftp://example.com/replay.zip")]
    [TestCase("file:///local/path/replay.zip")]
    public void DownloadReplayAsync_InvalidScheme_ThrowsArgumentException(string invalidUrl)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_dir_{Guid.NewGuid():N}");
        try
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await ReplayDownloader.DownloadReplayAsync(invalidUrl, tempDir));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
