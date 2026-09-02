using System;
using System.Collections.Generic;
using SS14.Launcher.Utility.Algorithms.Caching;
using SS14.Launcher.Utility.Algorithms.Filters;
using SS14.Launcher.Utility.Algorithms.Network;
using SS14.Launcher.Utility.Algorithms.RateLimiting;
using SS14.Launcher.Utility.Algorithms.Search;
using SS14.Launcher.Utility.Algorithms.Statistics;

namespace SS14.Launcher.Utility;

/// <summary>
/// Facade providing access to high-performance algorithmic primitives for server scoring,
/// latency tracking, and SIMD text search.
/// </summary>
public static class AdvancedAlgorithms
{
    /// <summary>
    /// Computes jitter-adaptive exponential moving average (EMA) for ping smoothing.
    /// </summary>
    public static float SmoothPingAdaptive(float currentSmooth, float newSample)
    {
        if (currentSmooth <= 0.001f)
            return MathF.Max(1f, newSample);

        var jitter = MathF.Abs(newSample - currentSmooth);
        var alpha = Math.Clamp(0.20f + 0.60f * (jitter / 120f), 0.15f, 0.80f);

        return (alpha * newSample) + ((1f - alpha) * currentSmooth);
    }

    // Type aliases for backward compatibility
    public sealed class KalmanLatencyTracker : Algorithms.Filters.KalmanLatencyTracker
    {
        public KalmanLatencyTracker(float processNoiseQ = 0.5f) : base(processNoiseQ) { }
    }

    public sealed class PSquareQuantileEstimator : Algorithms.Statistics.PSquareQuantileEstimator
    {
        public PSquareQuantileEstimator(double p = 0.50) : base(p) { }
    }

    public sealed class RunningStatistics : Algorithms.Statistics.RunningStatistics
    {
    }

    public sealed class HoltLinearTrend : Algorithms.Statistics.HoltLinearTrend
    {
        public HoltLinearTrend(double alpha = 0.3, double beta = 0.1) : base(alpha, beta) { }
    }

    public sealed class TokenBucket : Algorithms.RateLimiting.TokenBucket
    {
        public TokenBucket(double capacity, double refillRatePerSecond) : base(capacity, refillRatePerSecond) { }
    }

    public sealed class HybridMemoryCache<TKey, TValue> : Algorithms.Caching.HybridMemoryCache<TKey, TValue> where TKey : notnull
    {
        public HybridMemoryCache(int capacity = 500, TimeSpan? itemTtl = null) : base(capacity, itemTtl) { }
    }

    public sealed class ThroughputEtaEstimator : Algorithms.Network.ThroughputEtaEstimator
    {
        public ThroughputEtaEstimator(double smoothingAlpha = 0.25) : base(smoothingAlpha) { }
    }

    public sealed class FastServerSearchIndex<T> : Algorithms.Search.FastServerSearchIndex<T>
    {
    }

    public sealed class FastServerSearchIndex : Algorithms.Search.FastServerSearchIndex
    {
    }

    public static class SimdStringHelper
    {
        public static void ToLowerAsciiSimd(ReadOnlySpan<char> source, Span<char> destination, bool forceScalar = false)
            => Algorithms.Search.SimdStringHelper.ToLowerAsciiSimd(source, destination, forceScalar);

        public static string ToLowerAsciiSimd(string text, bool forceScalar = false)
            => Algorithms.Search.SimdStringHelper.ToLowerAsciiSimd(text, forceScalar);
    }

    public static double JaroWinklerSimilarity(string s1, string s2)
        => StringMetrics.JaroWinklerSimilarity(s1, s2);

    public static int DamerauLevenshteinDistance(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
        => StringMetrics.DamerauLevenshteinDistance(s, t);

    public static int MyersBitParallelDistance(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
        => StringMetrics.MyersBitParallelDistance(pattern, text);

    public static int MyersEditDistance(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
        => StringMetrics.MyersBitParallelDistance(pattern, text);

    public static double TrigramCosineSimilarity(string s1, string s2)
        => StringMetrics.TrigramCosineSimilarity(s1, s2);

    public static double OkapiBM25Score(string[] queryTerms, string targetDocument, double avgDocLength = 25.0, double k1 = 1.2, double b = 0.75)
        => StringMetrics.OkapiBM25Score(queryTerms, targetDocument, avgDocLength, k1, b);

    public static int FastBitwiseLevenshtein(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
        => StringMetrics.FastBitwiseLevenshtein(s, t);

    public static float CalculatePlayerVelocity(int currentPlayers, int previousPlayers, TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 1)
            return 0f;

        var delta = currentPlayers - previousPlayers;
        var minutes = (float)elapsed.TotalMinutes;
        return delta / MathF.Max(0.1f, minutes);
    }

    public enum ServerRegionCluster
    {
        RuCis,
        Europe,
        NorthAmerica,
        AsiaPacific,
        LatinAmerica,
        Global
    }

    public static ServerRegionCluster ClassifyRegion(string name, string tags, string languageCode)
    {
        var text = $"{name} {tags} {languageCode}".ToLowerInvariant();

        if (text.Contains("ru") || text.Contains("russian") || text.Contains("рус") || languageCode.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            return ServerRegionCluster.RuCis;

        if (text.Contains("latam") || text.Contains("es-") || text.Contains("pt-") || text.Contains("brazil") || text.Contains("испанский"))
            return ServerRegionCluster.LatinAmerica;

        if (text.Contains("eu") || text.Contains("europe") || text.Contains("germany") || text.Contains("france") || text.Contains("uk"))
            return ServerRegionCluster.Europe;

        if (text.Contains("us") || text.Contains("na") || text.Contains("america") || text.Contains("canada"))
            return ServerRegionCluster.NorthAmerica;

        if (text.Contains("asia") || text.Contains("sea") || text.Contains("jp") || text.Contains("cn") || text.Contains("kr"))
            return ServerRegionCluster.AsiaPacific;

        return ServerRegionCluster.Global;
    }

    public static double CalculatePredictiveQualityIndex(
        int playerCount,
        int maxPlayers,
        double? pingMilliseconds,
        double pingJitter,
        bool isFavorite,
        float playerVelocity = 0f,
        bool isInRound = true,
        bool isPanicBunker = false)
    {
        double score = 0.0;

        if (isFavorite)
            score += 2500.0;

        var maxCap = Math.Max(1, maxPlayers > 0 ? maxPlayers : 100);
        var ratio = Math.Clamp((double)playerCount / maxCap, 0.0, 1.0);

        if (playerCount == 0)
        {
            score += 5.0;
        }
        else
        {
            var occupancyFactor = Math.Exp(-Math.Pow(ratio - 0.72, 2) / (2.0 * 0.05));
            var capacityScore = 400.0 * occupancyFactor;
            var countScore = 250.0 * Math.Log2(1.0 + Math.Min(playerCount, 120));
            score += capacityScore + countScore;
        }

        if (pingMilliseconds.HasValue)
        {
            var effectiveLatency = Math.Max(1.0, pingMilliseconds.Value + (0.6 * pingJitter));
            var sigmoidScore = 500.0 / (1.0 + Math.Exp(0.032 * (effectiveLatency - 65.0)));
            score += sigmoidScore;
        }
        else
        {
            score += 60.0;
        }

        if (Math.Abs(playerVelocity) > 0.01f)
        {
            score += 60.0 * Math.Tanh(0.30f * playerVelocity);
        }

        if (isInRound)
        {
            score += 25.0;
        }

        if (isPanicBunker)
        {
            score -= 40.0;
        }

        return Math.Max(0.0, score);
    }
}
