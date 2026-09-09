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
    public void NormalizeDownloadUrl_TrimsAndRemovesTrailingQuestionMark()
    {
        var input = "  https://example.com/replays/round-123.zip?  ";
        var expected = "https://example.com/replays/round-123.zip";
        Assert.That(ReplayDownloader.NormalizeDownloadUrl(input), Is.EqualTo(expected));
    }

    [Test]
    public void NormalizeDownloadUrl_GenericUrl_Unchanged()
    {
        var input = "https://example.com/replays/round-123.zip";
        Assert.That(ReplayDownloader.NormalizeDownloadUrl(input), Is.EqualTo(input));
    }

    [Test]
    public void BuildUrlFromTemplate_Success()
    {
        var url = ReplayDownloader.BuildUrlFromTemplate("999", "https://myreplays.org/download/{roundId}.zip");
        Assert.That(url, Is.EqualTo("https://myreplays.org/download/999.zip"));

        var urlWithHash = ReplayDownloader.BuildUrlFromTemplate("#50135", "https://server.com/archive/{roundId}.zip");
        Assert.That(urlWithHash, Is.EqualTo("https://server.com/archive/50135.zip"));
    }

    [Test]
    public void BuildUrlFromTemplate_MissingPlaceholder_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ReplayDownloader.BuildUrlFromTemplate("999", "https://myreplays.org/download/archive.zip"));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("#")]
    [TestCase("###")]
    public void BuildUrlFromTemplate_InvalidRoundId_ThrowsArgumentException(string invalidId)
    {
        Assert.Throws<ArgumentException>(() =>
            ReplayDownloader.BuildUrlFromTemplate(invalidId, "https://myreplays.org/{roundId}.zip"));
    }

    [Test]
    public void BuildUrlFromTemplate_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ReplayDownloader.BuildUrlFromTemplate("100", null!));

        Assert.Throws<ArgumentException>(() =>
            ReplayDownloader.BuildUrlFromTemplate("100", ""));
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

    [Test]
    public void FormatEta_FormatsCorrectly()
    {
        Assert.That(ReplayDownloader.FormatEta(null), Is.EqualTo(""));
        Assert.That(ReplayDownloader.FormatEta(TimeSpan.FromSeconds(0)), Is.EqualTo(""));
        Assert.That(ReplayDownloader.FormatEta(TimeSpan.FromSeconds(45)), Is.EqualTo("~45 с."));
        Assert.That(ReplayDownloader.FormatEta(TimeSpan.FromSeconds(95)), Is.EqualTo("~1 мин. 35 с."));
        Assert.That(ReplayDownloader.FormatEta(TimeSpan.FromHours(1.5)), Is.EqualTo("~1 ч. 30 мин."));
    }

    [Test]
    public void DownloadReplayViewModel_Validation_WorkCorrectly()
    {
        var vm = new SS14.Launcher.ViewModels.DownloadReplayViewModel(Path.GetTempPath());

        // Default mode is by URL
        Assert.That(vm.IsByUrl, Is.True);
        Assert.That(vm.IsByRoundId, Is.False);
        Assert.That(vm.CanDownload, Is.False);

        // Valid URL
        vm.UrlInput = "https://example.com/replay.zip";
        Assert.That(vm.CanDownload, Is.True);

        // Invalid URL
        vm.UrlInput = "not-a-valid-url";
        Assert.That(vm.CanDownload, Is.False);

        // Switch to ByRoundId
        vm.IsByRoundId = true;
        Assert.That(vm.IsByUrl, Is.False);
        Assert.That(vm.CanDownload, Is.False);

        // Set Round ID with valid custom template
        vm.RoundIdInput = "50135";
        vm.CustomTemplateInput = "https://server.com/round_{roundId}.zip";
        Assert.That(vm.CanDownload, Is.True);

        // Template without {roundId} marker
        vm.CustomTemplateInput = "https://server.com/round.zip";
        Assert.That(vm.CanDownload, Is.False);
    }
}
