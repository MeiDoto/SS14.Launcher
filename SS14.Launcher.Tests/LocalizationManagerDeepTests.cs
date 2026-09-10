#nullable enable
using System;
using System.Globalization;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class LocalizationManagerDeepTests
{
    private DataManager _dataManager = null!;
    private LocalizationManager _loc = null!;

    [SetUp]
    public void SetUp()
    {
        _dataManager = new DataManager();
        _loc = new LocalizationManager(_dataManager);
        _loc.Initialize();
    }

    [AvaloniaTest]
    public void TestLocalizationManager_GetString_MissingKey_ReturnsKey()
    {
        var nonExistent = "this-key-definitely-does-not-exist-xyz123";
        var result = _loc.GetString(nonExistent);
        Assert.That(result, Is.EqualTo(nonExistent));
    }

    [AvaloniaTest]
    public void TestLocalizationManager_GetStringWithArgs_Formatting()
    {
        // Missing key with args should return the key itself as fallback
        var nonExistent = "missing-key-with-args";
        var result = _loc.GetString(nonExistent, ("name", "Station"), ("count", 42), ("ratio", 3.14f), ("nullVal", null));
        Assert.That(result, Is.EqualTo(nonExistent));
    }

    [AvaloniaTest]
    public void TestLocalizationManager_SwitchLanguage_InvokesEvent()
    {
        bool eventFired = false;
        _loc.LanguageSwitched += () => eventFired = true;

        _loc.SwitchToLanguage(new CultureInfo("ru"));
        Assert.That(eventFired, Is.True);
        Assert.That(_loc.CurrentCulture.TwoLetterISOLanguageName, Is.EqualTo("ru"));

        eventFired = false;
        _loc.SwitchToLanguage(new CultureInfo("en"));
        Assert.That(eventFired, Is.True);
        Assert.That(_loc.CurrentCulture.TwoLetterISOLanguageName, Is.EqualTo("en"));
    }

    [AvaloniaTest]
    public void TestLanguageInfo_RecordProperties()
    {
        var langRu = new LocalizationManager.LanguageInfo("ru");
        var langRu2 = new LocalizationManager.LanguageInfo("ru");
        var langEn = new LocalizationManager.LanguageInfo("en");

        Assert.That(langRu, Is.EqualTo(langRu2));
        Assert.That(langRu.Name, Is.EqualTo("ru"));
        Assert.That(langRu.Culture.TwoLetterISOLanguageName, Is.EqualTo("ru"));
        Assert.That(langRu, Is.Not.EqualTo(langEn));

        // Invalid culture name should safely fallback to fallback culture
        var invalid = new LocalizationManager.LanguageInfo("invalid-culture-!@#$");
        Assert.That(invalid.Culture, Is.Not.Null);
    }

    [AvaloniaTest]
    public void TestLocExtension_ProvideValue()
    {
        var ext = new LocExtension("test-key");
        Assert.That(ext.Key, Is.EqualTo("test-key"));

        var val = ext.ProvideValue(null!);
        Assert.That(val, Is.Not.Null);
    }
}
