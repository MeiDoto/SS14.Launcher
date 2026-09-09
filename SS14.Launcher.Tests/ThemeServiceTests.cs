#nullable enable
using System.IO;
using NUnit.Framework;
using Splat;
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

    [Test]
    public void TestThemeService_SplatRegistration_ResolvesInterface()
    {
        Splat.Locator.CurrentMutable.RegisterConstant<IThemeService>(ThemeService.Instance);
        var resolved = Splat.Locator.Current.GetService<IThemeService>();
        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved, Is.SameAs(ThemeService.Instance));
    }

    [Test]
    public void TestLocaleParity_EnUsAndRu_HaveIdenticalKeys()
    {
        // Load both locale files and extract message identifiers
        var baseDir = TestContext.CurrentContext.TestDirectory;
        var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "../../../.."));
        var enPath = Path.Combine(projectRoot, "SS14.Launcher/Assets/Locale/en-US/text.ftl");
        var ruPath = Path.Combine(projectRoot, "SS14.Launcher/Assets/Locale/ru/text.ftl");

        if (!File.Exists(enPath) || !File.Exists(ruPath))
            Assert.Ignore("Locale files not found relative to test directory.");

        static System.Collections.Generic.HashSet<string> ParseKeys(string path)
        {
            var keys = new System.Collections.Generic.HashSet<string>();
            foreach (var line in File.ReadLines(path))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"^([a-zA-Z0-9_-]+)\s*=");
                if (match.Success)
                    keys.Add(match.Groups[1].Value);
            }
            return keys;
        }

        var enKeys = ParseKeys(enPath);
        var ruKeys = ParseKeys(ruPath);

        Assert.That(enKeys.Count, Is.GreaterThan(800));
        Assert.That(ruKeys.Count, Is.GreaterThan(800));
        Assert.That(ruKeys, Is.EquivalentTo(enKeys), "RU and EN-US locale keys must have 100% parity.");
    }
}
