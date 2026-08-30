using System;
using System.Collections.Generic;
using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class AdvancedAlgorithmsTest
{
    [Test]
    public void TestMyersBitParallelDistance()
    {
        // Exact match
        Assert.That(AdvancedAlgorithms.MyersBitParallelDistance("space", "space"), Is.EqualTo(0));
        Assert.That(AdvancedAlgorithms.MyersBitParallelDistance("Station", "station"), Is.EqualTo(0));

        // 1 edit
        Assert.That(AdvancedAlgorithms.MyersBitParallelDistance("station", "statio"), Is.EqualTo(1));
        Assert.That(AdvancedAlgorithms.MyersBitParallelDistance("station", "stations"), Is.EqualTo(1));
        Assert.That(AdvancedAlgorithms.MyersBitParallelDistance("station", "statian"), Is.EqualTo(1));

        // Cyrillic
        Assert.That(AdvancedAlgorithms.MyersBitParallelDistance("Бункер", "бункер"), Is.EqualTo(0));
        Assert.That(AdvancedAlgorithms.MyersBitParallelDistance("Станция", "Станцыя"), Is.EqualTo(1));

        // Empty strings
        Assert.That(AdvancedAlgorithms.MyersBitParallelDistance("", "abc"), Is.EqualTo(3));
        Assert.That(AdvancedAlgorithms.MyersBitParallelDistance("abc", ""), Is.EqualTo(3));
        Assert.That(AdvancedAlgorithms.MyersBitParallelDistance("", ""), Is.EqualTo(0));
    }

    [Test]
    public void TestFastServerSearchIndex()
    {
        var index = new AdvancedAlgorithms.FastServerSearchIndex<string>();
        var entries = new List<(string Name, string Item)>
        {
            ("Space Station 14 Official", "official_1"),
            ("Corvax Sandbox [RU]", "corvax_sb"),
            ("Corvax Main Roleplay [RU]", "corvax_main"),
            ("WhiteDream Space RP", "wd_rp"),
            ("SS220 Station Server", "ss220")
        };

        index.Rebuild(entries);

        // Search by prefix
        var results = index.Search("Corvax");
        Assert.That(results, Contains.Item("corvax_sb"));
        Assert.That(results, Contains.Item("corvax_main"));

        // Search by substring
        var sandboxResults = index.Search("Sandbox");
        Assert.That(sandboxResults, Contains.Item("corvax_sb"));

        // Search by Cyrillic
        var ruResults = index.Search("RU");
        Assert.That(ruResults, Contains.Item("corvax_sb"));
        Assert.That(ruResults, Contains.Item("corvax_main"));
    }

    [Test]
    public void TestKalmanLatencyTracker()
    {
        var tracker = new AdvancedAlgorithms.KalmanLatencyTracker();

        // Feed steady 50ms samples
        for (int i = 0; i < 10; i++)
        {
            tracker.Update(50.0f);
        }

        Assert.That(tracker.EstimatedPing, Is.InRange(45.0f, 55.0f));

        // Outlier spike to 500ms shouldn't explode the estimate
        tracker.Update(500.0f);
        Assert.That(tracker.EstimatedPing, Is.LessThan(150.0f));
    }
}

// Additional tests for SIMD string helper
[TestFixture]
public sealed class SimdStringHelperTest
{
    [Test]
    public void TestToLowerAsciiSimd_BasicLatin()
    {
        var source = "Hello WORLD 123 Test".AsSpan();
        Span<char> dest = stackalloc char[source.Length];
        AdvancedAlgorithms.SimdStringHelper.ToLowerAsciiSimd(source, dest);
        Assert.That(new string(dest), Is.EqualTo("hello world 123 test"));
    }

    [Test]
    public void TestToLowerAsciiSimd_AllUppercase()
    {
        var source = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".AsSpan();
        Span<char> dest = stackalloc char[source.Length];
        AdvancedAlgorithms.SimdStringHelper.ToLowerAsciiSimd(source, dest);
        Assert.That(new string(dest), Is.EqualTo("abcdefghijklmnopqrstuvwxyz"));
    }

    [Test]
    public void TestToLowerAsciiSimd_AlreadyLower()
    {
        var source = "already lower case".AsSpan();
        Span<char> dest = stackalloc char[source.Length];
        AdvancedAlgorithms.SimdStringHelper.ToLowerAsciiSimd(source, dest);
        Assert.That(new string(dest), Is.EqualTo("already lower case"));
    }

    [Test]
    public void TestToLowerAsciiSimd_EmptyString()
    {
        var source = ReadOnlySpan<char>.Empty;
        Span<char> dest = Span<char>.Empty;
        AdvancedAlgorithms.SimdStringHelper.ToLowerAsciiSimd(source, dest);
        Assert.That(dest.Length, Is.EqualTo(0));
    }

    [Test]
    public void TestToLowerAsciiSimd_MixedWithSymbols()
    {
        var source = "Server[RU] #14 - TEST".AsSpan();
        Span<char> dest = stackalloc char[source.Length];
        AdvancedAlgorithms.SimdStringHelper.ToLowerAsciiSimd(source, dest);
        Assert.That(new string(dest), Is.EqualTo("server[ru] #14 - test"));
    }
}
