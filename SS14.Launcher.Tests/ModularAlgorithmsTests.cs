using System;
using System.Linq;
using NUnit.Framework;
using SS14.Launcher.Utility.Algorithms.Caching;
using SS14.Launcher.Utility.Algorithms.Filters;
using SS14.Launcher.Utility.Algorithms.Network;
using SS14.Launcher.Utility.Algorithms.RateLimiting;
using SS14.Launcher.Utility.Algorithms.Search;
using SS14.Launcher.Utility.Algorithms.Statistics;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class ModularAlgorithmsTests
{
    [Test]
    public void TestKalmanLatencyTrackerConvergence()
    {
        var tracker = new KalmanLatencyTracker(processNoiseQ: 0.2f);
        (float ping, float jitter) result = default;

        // Feed constant 50ms with 0 noise
        for (int i = 0; i < 20; i++)
        {
            result = tracker.Update(50.0f);
        }

        Assert.That(result.ping, Is.EqualTo(50.0f).Within(2.0f));
        Assert.That(tracker.EstimatedPing, Is.EqualTo(50.0f).Within(2.0f));
        Assert.That(tracker.EstimatedJitter, Is.LessThan(5.0f));
    }

    [Test]
    public void TestRunningStatisticsWelford()
    {
        var stats = new RunningStatistics();
        double[] values = [10.0, 20.0, 30.0, 40.0, 50.0];

        foreach (var v in values)
        {
            stats.Push(v);
        }

        Assert.That(stats.Count, Is.EqualTo(5));
        Assert.That(stats.Mean, Is.EqualTo(30.0).Within(0.001));
        Assert.That(stats.Min, Is.EqualTo(10.0));
        Assert.That(stats.Max, Is.EqualTo(50.0));
        Assert.That(stats.Variance, Is.EqualTo(250.0).Within(0.001));
        Assert.That(stats.StandardDeviation, Is.EqualTo(Math.Sqrt(250.0)).Within(0.001));

        stats.Reset();
        Assert.That(stats.Count, Is.EqualTo(0));
        Assert.That(stats.Mean, Is.EqualTo(0.0));
    }

    [Test]
    public void TestPSquareQuantileMedian()
    {
        var pSquare = new PSquareQuantileEstimator(p: 0.50);
        for (int i = 1; i <= 100; i++)
        {
            pSquare.Add(i);
        }

        Assert.That(pSquare.SampleCount, Is.EqualTo(100));
        Assert.That(pSquare.Estimate(), Is.EqualTo(50.0).Within(5.0));
    }

    [Test]
    public void TestHoltLinearTrendForecasting()
    {
        var trend = new HoltLinearTrend(alpha: 0.4, beta: 0.2);
        // Linear slope: 10, 20, 30, 40, 50
        for (int i = 1; i <= 10; i++)
        {
            trend.Update(i * 10.0);
        }

        Assert.That(trend.TrendVelocity, Is.GreaterThan(5.0));
        var forecastNext = trend.Forecast(1);
        Assert.That(forecastNext, Is.GreaterThan(100.0));
    }

    [Test]
    public void TestTokenBucketRateLimiting()
    {
        var bucket = new TokenBucket(capacity: 3, refillRatePerSecond: 10);
        Assert.That(bucket.TryConsume(1), Is.True);
        Assert.That(bucket.TryConsume(1), Is.True);
        Assert.That(bucket.TryConsume(1), Is.True);
        Assert.That(bucket.TryConsume(1), Is.False);
    }

    [Test]
    public void TestHybridMemoryCacheLru()
    {
        // Min capacity is 10 (enforced by Math.Max(10, capacity))
        var cache = new HybridMemoryCache<string, int>(capacity: 10, itemTtl: TimeSpan.FromMinutes(5));

        // Fill to capacity
        for (int i = 0; i < 10; i++)
        {
            cache.Set($"K{i}", i);
        }

        Assert.That(cache.Count, Is.EqualTo(10));

        // Access K0 to move it to MRU (front)
        Assert.That(cache.TryGet("K0", out var valK0), Is.True);
        Assert.That(valK0, Is.EqualTo(0));

        // Insert K10 -> K1 should be evicted (oldest not-recently-accessed)
        cache.Set("K10", 10);
        Assert.That(cache.TryGet("K0", out _), Is.True);   // recently accessed, should survive
        Assert.That(cache.TryGet("K10", out _), Is.True);   // just inserted
        Assert.That(cache.TryGet("K1", out _), Is.False);   // LRU evicted
    }

    [Test]
    public void TestThroughputEtaEstimatorEta()
    {
        var etaEstimator = new ThroughputEtaEstimator(smoothingAlpha: 0.5);
        etaEstimator.Update(0, 1000);
        System.Threading.Thread.Sleep(150);
        var eta = etaEstimator.Update(500, 1000);

        Assert.That(eta, Is.Not.Null);
        Assert.That(etaEstimator.BytesPerSecond, Is.GreaterThan(0));
    }

    [Test]
    public void TestFastServerSearchIndexQuery()
    {
        var index = new FastServerSearchIndex<int>();
        index.Rebuild([
            ("Corvax Sandbox Server", 1),
            ("WhiteDream Space Station 14", 2),
            ("Garrys Mod Server", 3)
        ]);

        var resultCorvax = index.Search("corvax");
        Assert.That(resultCorvax, Does.Contain(1));
        Assert.That(resultCorvax, Does.Not.Contain(3));

        var resultStation = index.Search("station");
        Assert.That(resultStation, Does.Contain(2));
    }

    [Test]
    public void TestStringMetricsLevenshteinAndMyers()
    {
        // Same strings -> distance 0
        Assert.That(StringMetrics.DamerauLevenshteinDistance("Corvax", "Corvax"), Is.EqualTo(0));
        Assert.That(StringMetrics.MyersBitParallelDistance("Corvax", "Corvax"), Is.EqualTo(0));

        // "cat" vs "car" -> distance 1
        Assert.That(StringMetrics.DamerauLevenshteinDistance("cat", "car"), Is.EqualTo(1));

        // Verify distances are non-negative
        var d1 = StringMetrics.DamerauLevenshteinDistance("Corvax", "Corvax Sandbox");
        var m1 = StringMetrics.MyersBitParallelDistance("Corvax", "Corvax Sandbox");
        Assert.That(d1, Is.GreaterThan(0));
        Assert.That(m1, Is.GreaterThan(0));
        // Note: DamerauLevenshtein (allows transpositions) and Myers (pure edit distance)
        // may return different values — that's expected

        var similarity = StringMetrics.JaroWinklerSimilarity("Station", "Station 14");
        Assert.That(similarity, Is.GreaterThan(0.85));
    }

    [Test]
    public void TestSimdStringHelperConversion()
    {
        var input = "The Quick Brown FOX Jumps Over The LAZY Dog 12345!";
        var simdResult = SimdStringHelper.ToLowerAsciiSimd(input);
        Assert.That(simdResult, Is.EqualTo(input.ToLowerInvariant()));
    }
}
