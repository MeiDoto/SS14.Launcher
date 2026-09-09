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
    public void BuildUrl_WizDen_StandardRoundId()
    {
        var url = ReplayDownloader.BuildUrlFromRoundId("14290", ReplayProviderPreset.WizDenOfficial);
        Assert.That(url, Is.EqualTo("https://replays.spacestation14.com/replays/round-14290.zip"));
    }

    [Test]
    public void BuildUrl_WizDen_WithLeadingHash()
    {
        var url = ReplayDownloader.BuildUrlFromRoundId("#14290", ReplayProviderPreset.WizDenOfficial);
        Assert.That(url, Is.EqualTo("https://replays.spacestation14.com/replays/round-14290.zip"));
    }

    [Test]
    public void BuildUrl_Corvax_StandardRoundId()
    {
        var url = ReplayDownloader.BuildUrlFromRoundId("501", ReplayProviderPreset.Corvax);
        Assert.That(url, Is.EqualTo("https://corvax.fun/replays/round_501.zip"));
    }

    [Test]
    public void BuildUrl_Corvax_WithLeadingHash()
    {
        var url = ReplayDownloader.BuildUrlFromRoundId("#501", ReplayProviderPreset.Corvax);
        Assert.That(url, Is.EqualTo("https://corvax.fun/replays/round_501.zip"));
    }

    [Test]
    public void BuildUrl_CustomTemplate_Success()
    {
        var url = ReplayDownloader.BuildUrlFromRoundId("999", ReplayProviderPreset.CustomTemplate, "https://myreplays.org/download/{roundId}.zip");
        Assert.That(url, Is.EqualTo("https://myreplays.org/download/999.zip"));
    }

    [Test]
    public void BuildUrl_CustomTemplate_MissingPlaceholder_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ReplayDownloader.BuildUrlFromRoundId("999", ReplayProviderPreset.CustomTemplate, "https://myreplays.org/download/archive.zip"));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("#")]
    [TestCase("###")]
    public void BuildUrl_InvalidRoundId_ThrowsArgumentException(string invalidId)
    {
        Assert.Throws<ArgumentException>(() =>
            ReplayDownloader.BuildUrlFromRoundId(invalidId, ReplayProviderPreset.WizDenOfficial));
    }

    [Test]
    public void BuildUrl_CustomTemplate_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ReplayDownloader.BuildUrlFromRoundId("100", ReplayProviderPreset.CustomTemplate, null));

        Assert.Throws<ArgumentException>(() =>
            ReplayDownloader.BuildUrlFromRoundId("100", ReplayProviderPreset.CustomTemplate, ""));
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
