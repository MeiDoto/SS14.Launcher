using System.IO;
using NUnit.Framework;
using SS14.Launcher.Models.ContentManagement;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class StorageAnalyzerTests
{
    [TestCase(0L, "0 B")]
    [TestCase(512L, "512 B")]
    [TestCase(1024L, "1 KB")]
    [TestCase(1536L, "2 KB")]
    [TestCase(1048576L, "1.0 MB")]
    [TestCase(52428800L, "50.0 MB")]
    [TestCase(1073741824L, "1.00 GB")]
    [TestCase(2684354560L, "2.50 GB")]
    public void TestFormatBytes(long bytes, string expected)
    {
        var formatted = StorageAnalyzer.FormatBytes(bytes);
        Assert.That(formatted, Is.EqualTo(expected));
    }

    [Test]
    public void TestStorageBreakdownSummation()
    {
        var breakdown = new StorageBreakdown
        {
            ContentDbBytes = 500 * 1024 * 1024,
            ContentWalBytes = 50 * 1024 * 1024,
            EnginesTotalBytes = 200 * 1024 * 1024,
            LogsTotalBytes = 10 * 1024 * 1024,
            ReplaysTotalBytes = 150 * 1024 * 1024
        };

        Assert.That(breakdown.TotalBytes, Is.EqualTo(910L * 1024 * 1024));
        Assert.That(breakdown.FormattedTotal, Is.EqualTo("910.0 MB"));
        Assert.That(breakdown.FormattedContentDb, Is.EqualTo("550.0 MB"));
        Assert.That(breakdown.FormattedEngines, Is.EqualTo("200.0 MB"));
        Assert.That(breakdown.FormattedLogs, Is.EqualTo("10.0 MB"));
        Assert.That(breakdown.FormattedReplays, Is.EqualTo("150.0 MB"));
    }

    [Test]
    public void TestGetDirectorySize()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ss14_storage_test_{Path.GetRandomFileName()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllBytes(Path.Combine(tempDir, "file1.bin"), new byte[100]);
            var subDir = Path.Combine(tempDir, "subdir");
            Directory.CreateDirectory(subDir);
            File.WriteAllBytes(Path.Combine(subDir, "file2.bin"), new byte[250]);

            var size = StorageAnalyzer.GetDirectorySize(new DirectoryInfo(tempDir));
            Assert.That(size, Is.EqualTo(350));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
