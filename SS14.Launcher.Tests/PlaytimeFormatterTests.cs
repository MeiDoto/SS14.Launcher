#nullable enable

using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public sealed class PlaytimeFormatterTests
{
    [Test]
    public void TestPluralizationRussian()
    {
        Assert.That(PlaytimeFormatter.FormatPlaytime(0, true), Is.EqualTo("0 минут"));
        Assert.That(PlaytimeFormatter.FormatPlaytime(60, true), Is.EqualTo("1 минута"));
        Assert.That(PlaytimeFormatter.FormatPlaytime(120, true), Is.EqualTo("2 минуты"));
        Assert.That(PlaytimeFormatter.FormatPlaytime(300, true), Is.EqualTo("5 минут"));
        Assert.That(PlaytimeFormatter.FormatPlaytime(3600, true), Is.EqualTo("1 час"));
        Assert.That(PlaytimeFormatter.FormatPlaytime(7200, true), Is.EqualTo("2 часа"));
        Assert.That(PlaytimeFormatter.FormatPlaytime(18000, true), Is.EqualTo("5 часов"));
        Assert.That(PlaytimeFormatter.FormatPlaytime(7500, true), Is.EqualTo("2 часа 5 минут"));
    }

    [Test]
    public void TestPluralizationEnglish()
    {
        Assert.That(PlaytimeFormatter.FormatPlaytime(0, false), Is.EqualTo("0 minutes"));
        Assert.That(PlaytimeFormatter.FormatPlaytime(60, false), Is.EqualTo("1 minute"));
        Assert.That(PlaytimeFormatter.FormatPlaytime(120, false), Is.EqualTo("2 minutes"));
        Assert.That(PlaytimeFormatter.FormatPlaytime(3600, false), Is.EqualTo("1 hour"));
        Assert.That(PlaytimeFormatter.FormatPlaytime(7200, false), Is.EqualTo("2 hours"));
        Assert.That(PlaytimeFormatter.FormatPlaytime(7500, false), Is.EqualTo("2 hours 5 minutes"));
    }
}
