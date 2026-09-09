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

    [Test]
    public void TestApplyTheme_CustomBackground_SetsSemiTransparentBrushes()
    {
        var dict = new Avalonia.Controls.ResourceDictionary();
        var dm = new SS14.Launcher.Models.Data.DataManager();
        ThemeService.Instance.ApplyTheme(dict, dm, hasCustomBackground: true);

        Assert.That(dict.ContainsKey("ThemeServerListBackgroundBrush"), Is.True);
        Assert.That(dict.ContainsKey("ThemeServerListRowAltBrush"), Is.True);

        var bgBrush = (Avalonia.Media.ISolidColorBrush)dict["ThemeServerListBackgroundBrush"]!;
        var altBrush = (Avalonia.Media.ISolidColorBrush)dict["ThemeServerListRowAltBrush"]!;

        Assert.That(bgBrush.Color.A, Is.EqualTo(0x28));
        Assert.That(altBrush.Color.A, Is.EqualTo(0x38));
    }

    [Test]
    public void TestApplyTheme_NoCustomBackground_SetsOpaqueBrushes()
    {
        var dict = new Avalonia.Controls.ResourceDictionary();
        var dm = new SS14.Launcher.Models.Data.DataManager();
        ThemeService.Instance.ApplyTheme(dict, dm, hasCustomBackground: false);

        Assert.That(dict.ContainsKey("ThemeServerListBackgroundBrush"), Is.True);
        Assert.That(dict.ContainsKey("ThemeServerListRowAltBrush"), Is.True);

        var bgBrush = (Avalonia.Media.ISolidColorBrush)dict["ThemeServerListBackgroundBrush"]!;
        var altBrush = (Avalonia.Media.ISolidColorBrush)dict["ThemeServerListRowAltBrush"]!;

        Assert.That(bgBrush.Color.A, Is.EqualTo(0xFF));
        Assert.That(altBrush.Color.A, Is.EqualTo(0xFF));
        Assert.That(bgBrush.Color, Is.EqualTo(Avalonia.Media.Color.FromRgb(0x1E, 0x1E, 0x22)));
        Assert.That(altBrush.Color, Is.EqualTo(Avalonia.Media.Color.FromRgb(0x26, 0x26, 0x26)));
    }
}
