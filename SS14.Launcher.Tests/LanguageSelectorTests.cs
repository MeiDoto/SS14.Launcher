#nullable enable
using System.Globalization;
using NUnit.Framework;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class LanguageSelectorTests
{
    [Test]
    public void TestCapitalizeFirst()
    {
        Assert.That(LanguageSelectorLanguageViewModel.CapitalizeFirst("русский"), Is.EqualTo("Русский"));
        Assert.That(LanguageSelectorLanguageViewModel.CapitalizeFirst("english"), Is.EqualTo("English"));
        Assert.That(LanguageSelectorLanguageViewModel.CapitalizeFirst(""), Is.EqualTo(""));
    }

    [Test]
    public void TestFormattedLanguageName_RussianUi()
    {
        var ruUi = new CultureInfo("ru");
        var ruCulture = new CultureInfo("ru");
        var enCulture = new CultureInfo("en");

        Assert.That(LanguageSelectorLanguageViewModel.GetFormattedLanguageName(ruCulture, ruUi), Is.EqualTo("Русский"));
        Assert.That(LanguageSelectorLanguageViewModel.GetFormattedLanguageName(enCulture, ruUi), Is.EqualTo("Английский"));
    }

    [Test]
    public void TestFormattedLanguageName_EnglishUi()
    {
        var enUi = new CultureInfo("en");
        var ruCulture = new CultureInfo("ru");
        var enCulture = new CultureInfo("en");

        Assert.That(LanguageSelectorLanguageViewModel.GetFormattedLanguageName(ruCulture, enUi), Is.EqualTo("Russian"));
        Assert.That(LanguageSelectorLanguageViewModel.GetFormattedLanguageName(enCulture, enUi), Is.EqualTo("English"));
    }

    [Test]
    public void TestFormattedDisplay_RussianUi()
    {
        var ruUi = new CultureInfo("ru");
        var ruCulture = new CultureInfo("ru");
        var enCulture = new CultureInfo("en");
        var loc = new LocalizationManager(new DataManager());

        var enDisplay = LanguageSelectorLanguageViewModel.GetFormattedDisplay(enCulture, ruUi, loc);
        var ruDisplay = LanguageSelectorLanguageViewModel.GetFormattedDisplay(ruCulture, ruUi, loc);

        Assert.That(enDisplay, Is.EqualTo("Английский (English)"));
        Assert.That(ruDisplay, Is.EqualTo("Русский (Russian)"));
    }

    [Test]
    public void TestFormattedDisplay_EnglishUi()
    {
        var enUi = new CultureInfo("en");
        var ruCulture = new CultureInfo("ru");
        var enCulture = new CultureInfo("en");
        var loc = new LocalizationManager(new DataManager());

        var enDisplay = LanguageSelectorLanguageViewModel.GetFormattedDisplay(enCulture, enUi, loc);
        var ruDisplay = LanguageSelectorLanguageViewModel.GetFormattedDisplay(ruCulture, enUi, loc);

        Assert.That(enDisplay, Is.EqualTo("English (English)"));
        Assert.That(ruDisplay, Is.EqualTo("Russian (Russian Federation)"));
    }
}
