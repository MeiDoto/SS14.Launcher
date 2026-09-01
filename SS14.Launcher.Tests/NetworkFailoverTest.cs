using System;
using System.Collections.Immutable;
using System.Net;
using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class NetworkFailoverTest
{
    [Test]
    public void TestUrlFallbackSetStatsTracking()
    {
        var stats = new UrlFallbackSetStats(3);

        // Record successes
        stats.AddSuccessfulRequest(0);
        stats.AddSuccessfulRequest(0);
        stats.AddSuccessfulRequest(1);

        Assert.That(stats.RequestCount[0], Is.EqualTo(2));
        Assert.That(stats.RequestCount[1], Is.EqualTo(1));
        Assert.That(stats.RequestCount[2], Is.EqualTo(0));
    }

    [Test]
    public void TestUrlFallbackSetValidationAndMostSuccessful()
    {
        // Empty array should throw
        Assert.Throws<ArgumentException>(() => new UrlFallbackSet(ImmutableArray<string>.Empty));

        // Valid URLs should construct
        var stats = new UrlFallbackSetStats(2);
        var set = new UrlFallbackSet(new[] { "https://primary.example.com", "https://backup.example.com" }.ToImmutableArray(), stats);
        Assert.That(set.Urls.Length, Is.EqualTo(2));
        Assert.That(set.Urls[0], Is.EqualTo("https://primary.example.com"));
        Assert.That(set.Urls[1], Is.EqualTo("https://backup.example.com"));

        stats.AddSuccessfulRequest(1);
        stats.AddSuccessfulRequest(1);
        Assert.That(set.GetMostSuccessfulUrl(), Is.EqualTo("https://backup.example.com"));
    }

    [Test]
    public void TestHappyEyeballsInterleaveSorting()
    {
        var ips = new[]
        {
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("10.0.0.1"),
            IPAddress.Parse("::1"),
            IPAddress.Parse("fe80::1")
        };

        var sorted = HappyEyeballsHttp.SortInterleaved(ips);

        Assert.That(sorted.Length, Is.EqualTo(4));
        // Should interleave IPv6 and IPv4
        Assert.That(sorted[0].AddressFamily, Is.EqualTo(System.Net.Sockets.AddressFamily.InterNetworkV6));
        Assert.That(sorted[1].AddressFamily, Is.EqualTo(System.Net.Sockets.AddressFamily.InterNetwork));
    }
}
