#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class AlgorithmsDeepTests
{
    [Test]
    public void TestKalmanLatencyTracker_SuppressesSpikes()
    {
        var tracker = new AdvancedAlgorithms.KalmanLatencyTracker();
        
        // Initial clean samples around 50ms
        for (int i = 0; i < 10; i++)
        {
            tracker.Update(50f);
        }
        
        Assert.That(tracker.EstimatedPing, Is.InRange(45f, 55f));

        // Inject an extreme spike (e.g. 500ms momentary hiccup)
        var (smoothedPing, jitter) = tracker.Update(500f);

        // Chi-squared gating should suppress the spike from blowing up the estimate
        Assert.That(smoothedPing, Is.LessThan(200f));
        Assert.That(jitter, Is.GreaterThan(0f));
    }

    [Test]
    public void TestKalmanLatencyTracker_TracksGradualChange()
    {
        var tracker = new AdvancedAlgorithms.KalmanLatencyTracker();

        // Feed steady 40ms
        for (int i = 0; i < 15; i++)
            tracker.Update(40f);

        // Feed steady 120ms to allow convergence
        for (int i = 0; i < 80; i++)
            tracker.Update(120f);

        // Final estimate should smoothly track up to around 120ms
        Assert.That(tracker.EstimatedPing, Is.InRange(100f, 130f));
    }

    [Test]
    public void TestPSquareQuantileEstimator_CalculatesMedian()
    {
        var p2 = new AdvancedAlgorithms.PSquareQuantileEstimator(0.50);

        // Add 100 values from 1 to 100
        for (int i = 1; i <= 100; i++)
        {
            p2.Add(i);
        }

        // Median of 1..100 should be close to 50.5
        var estimate = p2.Estimate();
        Assert.That(estimate, Is.InRange(40.0, 60.0));
        Assert.That(p2.SampleCount, Is.EqualTo(100));
    }

    [Test]
    public void TestPSquareQuantileEstimator_CalculatesP95()
    {
        var p95 = new AdvancedAlgorithms.PSquareQuantileEstimator(0.95);

        for (int i = 1; i <= 100; i++)
        {
            p95.Add(i);
        }

        // 95th percentile of 1..100 should be around 95
        var estimate = p95.Estimate();
        Assert.That(estimate, Is.InRange(85.0, 100.0));
    }

    [Test]
    public void TestRunningStatistics_WelfordAlgorithm()
    {
        var stats = new AdvancedAlgorithms.RunningStatistics();
        Assert.That(stats.Count, Is.EqualTo(0));
        Assert.That(stats.Mean, Is.EqualTo(0.0));
        Assert.That(stats.Variance, Is.EqualTo(0.0));

        var values = new[] { 10.0, 20.0, 30.0, 40.0, 50.0 };
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
    }

    [Test]
    public void TestHoltLinearTrend_Forecasting()
    {
        var trend = new AdvancedAlgorithms.HoltLinearTrend(alpha: 0.5, beta: 0.3);

        // Feed linear sequence: 10, 20, 30, 40, 50, 60, 70, 80, 90, 100
        for (int i = 1; i <= 20; i++)
        {
            trend.Update(i * 10.0);
        }

        // Forecast 1 step ahead (should be around 210)
        var forecast = trend.Forecast(1);
        Assert.That(forecast, Is.InRange(180.0, 230.0));
    }

    [Test]
    public void TestTokenBucket_RateLimiting()
    {
        // 5 tokens capacity, refill 5 per second
        var bucket = new AdvancedAlgorithms.TokenBucket(5.0, 5.0);

        // Can consume 5 immediately
        Assert.That(bucket.TryConsume(5.0), Is.True);

        // 6th consume should fail because bucket is empty
        Assert.That(bucket.TryConsume(1.0), Is.False);
    }

    [Test]
    public void TestThroughputEtaEstimator_SmoothSpeed()
    {
        var estimator = new AdvancedAlgorithms.ThroughputEtaEstimator();

        var eta = estimator.Update(1000, 10000);
        var speed = estimator.BytesPerSecond;
        Assert.That(speed, Is.GreaterThanOrEqualTo(0.0));
    }

    [Test]
    public void TestFastServerSearchIndex_QueryAndScore()
    {
        var index = new AdvancedAlgorithms.FastServerSearchIndex<string>();
        index.Rebuild(new[]
        {
            ("Corvax Sandbox Roleplay", "server-1"),
            ("Space Station 14 Official", "server-2")
        });

        var results = index.Search("Corvax", maxResults: 5);
        Assert.That(results.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(results[0], Is.EqualTo("server-1"));
    }

    [Test]
    public void TestSimdStringHelper_ScalarAndSimdEquivalence()
    {
        var testStrings = new[]
        {
            "",
            "A",
            "HELLO",
            "Space Station 14 - Official Server [Roleplay / Corvax] 2026!",
            "THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG 1234567890!@#$%^&*()_+",
            "abcdefghijklmnopqrstuvwxyz",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        };

        foreach (var s in testStrings)
        {
            var destSimd = new char[s.Length];
            var destScalar = new char[s.Length];

            AdvancedAlgorithms.SimdStringHelper.ToLowerAsciiSimd(s.AsSpan(), destSimd.AsSpan(), forceScalar: false);
            AdvancedAlgorithms.SimdStringHelper.ToLowerAsciiSimd(s.AsSpan(), destScalar.AsSpan(), forceScalar: true);

            var strSimd = new string(destSimd);
            var strScalar = new string(destScalar);

            Assert.That(strSimd, Is.EqualTo(strScalar), $"Mismatch for input: '{s}'");
        }
    }
}
