using System;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;

namespace SS14.Launcher.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public sealed class ZipSlipSecurityTests
{
    [Test]
    public void TestExtractZipToDirectory_BlocksZipSlip()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"zip_slip_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry("../evil.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("malicious payload");
            }

            ms.Position = 0;

            Assert.Throws<InvalidDataException>(() =>
            {
                Helpers.ExtractZipToDirectory(tempDir, ms);
            });
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Test]
    public void TestExtractZipToDirectory_ExtractsValidFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"zip_valid_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry("subfolder/hello.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("hello world");
            }

            ms.Position = 0;
            Helpers.ExtractZipToDirectory(tempDir, ms);

            var extractedFile = Path.Combine(tempDir, "subfolder", "hello.txt");
            Assert.That(File.Exists(extractedFile), Is.True);
            Assert.That(File.ReadAllText(extractedFile), Is.EqualTo("hello world"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
