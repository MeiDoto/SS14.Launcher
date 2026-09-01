#nullable enable
using System.IO;
using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class ThemeServiceTests
{
    [Test]
    public void TestLoadBitmapSafely_NonExistentFileReturnsNull()
    {
        var result = ThemeService.Instance.LoadBitmapSafely("non_existent_file_path_123.png");
        Assert.That(result, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void TestLoadBitmapSafely_NullOrEmptyReturnsNull(string? path)
    {
        var result = ThemeService.Instance.LoadBitmapSafely(path);
        Assert.That(result, Is.Null);
    }
}
