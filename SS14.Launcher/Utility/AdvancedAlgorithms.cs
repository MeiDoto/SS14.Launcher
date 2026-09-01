using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SS14.Launcher.Utility;

/// <summary>
/// High-performance algorithmic primitives for server scoring, latency tracking, and SIMD text search.
/// </summary>
public static class AdvancedAlgorithms
{
    /// <summary>
    /// Computes jitter-adaptive exponential moving average (EMA) for ping smoothing.
    /// </summary>
    /// <param name="currentSmooth">The current smoothed latency value in ms.</param>
    /// <param name="newSample">The latest measured round-trip time in ms.</param>
    /// <returns>Updated smoothed latency estimate in ms.</returns>
    public static float SmoothPingAdaptive(float currentSmooth, float newSample)
    {
        if (currentSmooth <= 0.001f)
            return MathF.Max(1f, newSample);

        var jitter = MathF.Abs(newSample - currentSmooth);
        var alpha = Math.Clamp(0.20f + 0.60f * (jitter / 120f), 0.15f, 0.80f);

        return (alpha * newSample) + ((1f - alpha) * currentSmooth);
    }

    /// <summary>
    /// 1D Kalman filter tracker with Chi-squared gating to filter latency spikes and estimate packet jitter.
    /// </summary>
    public sealed class KalmanLatencyTracker
    {
        private float _estimate;
        private float _errorCovariance = 100.0f;
        private readonly float _processNoiseQ;
        private float _measurementNoiseR = 25.0f;
        private float _jitterEma;
        private bool _initialized;
        private readonly object _lock = new();

        public float EstimatedPing => _estimate;
        public float EstimatedJitter => _jitterEma;

        public KalmanLatencyTracker(float processNoiseQ = 0.5f)
        {
            _processNoiseQ = MathF.Max(0.01f, processNoiseQ);
        }

        public (float Ping, float Jitter) Update(float measurement)
        {
            lock (_lock)
            {
                if (!_initialized)
                {
                    _estimate = MathF.Max(1.0f, measurement);
                    _jitterEma = 0.0f;
                    _errorCovariance = 10.0f;
                    _initialized = true;
                    return (_estimate, _jitterEma);
                }

                var predEstimate = _estimate;
                var predCov = _errorCovariance + _processNoiseQ;

                var innovation = measurement - predEstimate;
                var absResidual = MathF.Abs(innovation);

                // Chi-squared outlier gating (reject extreme latency spikes / lag bursts)
                var innovationVariance = predCov + _measurementNoiseR;
                var stdDev = MathF.Sqrt(innovationVariance);
                if (absResidual > 3.5f * stdDev && _initialized)
                {
                    innovation = MathF.Sign(innovation) * (3.5f * stdDev);
                }

                _jitterEma = (0.15f * absResidual) + (0.85f * _jitterEma);
                _measurementNoiseR = Math.Clamp((0.8f * _measurementNoiseR) + (0.2f * (innovation * innovation)), 4.0f, 400.0f);

                var kalmanGain = predCov / (predCov + _measurementNoiseR);
                _estimate = MathF.Max(1.0f, predEstimate + (kalmanGain * innovation));
                _errorCovariance = (1.0f - kalmanGain) * predCov;

                return (_estimate, _jitterEma);
            }
        }
    }

    public sealed class PSquareQuantileEstimator
    {
        private readonly double _p;
        private readonly double[] _q = new double[5];
        private readonly int[] _n = new int[5];
        private readonly double[] _np = new double[5];
        private readonly double[] _dn = new double[5];
        private int _count;
        private readonly object _lock = new();

        public double Quantile => _p;
        public int SampleCount => _count;

        public PSquareQuantileEstimator(double p = 0.50)
        {
            _p = Math.Clamp(p, 0.01, 0.99);
        }

        public void Add(double x)
        {
            lock (_lock)
            {
                if (_count < 5)
                {
                    _q[_count++] = x;
                    if (_count == 5)
                    {
                        Array.Sort(_q);
                        for (int i = 0; i < 5; i++)
                        {
                            _n[i] = i + 1;
                        }
                        _np[0] = 1;
                        _np[1] = 1 + 2 * _p;
                        _np[2] = 1 + 4 * _p;
                        _np[3] = 3 + 2 * _p;
                        _np[4] = 5;

                        _dn[0] = 0;
                        _dn[1] = _p / 2;
                        _dn[2] = _p;
                        _dn[3] = (1 + _p) / 2;
                        _dn[4] = 1;
                    }
                    return;
                }

                int k = -1;
                if (x < _q[0])
                {
                    _q[0] = x;
                    k = 0;
                }
                else if (x >= _q[4])
                {
                    _q[4] = x;
                    k = 3;
                }
                else
                {
                    for (int i = 1; i < 5; i++)
                    {
                        if (x < _q[i])
                        {
                            k = i - 1;
                            break;
                        }
                    }
                }

                for (int i = k + 1; i < 5; i++)
                {
                    _n[i]++;
                }

                for (int i = 0; i < 5; i++)
                {
                    _np[i] += _dn[i];
                }

                for (int i = 1; i <= 3; i++)
                {
                    double d = _np[i] - _n[i];
                    if ((d >= 1 && _n[i + 1] - _n[i] > 1) || (d <= -1 && _n[i - 1] - _n[i] < -1))
                    {
                        int sign = Math.Sign(d);
                        double qPrime = Parabolic(i, sign);
                        if (_q[i - 1] < qPrime && qPrime < _q[i + 1])
                        {
                            _q[i] = qPrime;
                        }
                        else
                        {
                            _q[i] = Linear(i, sign);
                        }
                        _n[i] += sign;
                    }
                }
                _count++;
            }
        }

        private double Parabolic(int i, double d)
        {
            return _q[i] + (d / (_n[i + 1] - _n[i - 1])) *
                ((_n[i] - _n[i - 1] + d) * (_q[i + 1] - _q[i]) / (_n[i + 1] - _n[i]) +
                 (_n[i + 1] - _n[i] - d) * (_q[i] - _q[i - 1]) / (_n[i] - _n[i - 1]));
        }

        private double Linear(int i, int d)
        {
            return _q[i] + d * (_q[i + d] - _q[i]) / (_n[i + d] - _n[i]);
        }

        public double Estimate()
        {
            lock (_lock)
            {
                if (_count == 0) return 0.0;
                if (_count <= 5)
                {
                    var copy = new double[_count];
                    Array.Copy(_q, copy, _count);
                    Array.Sort(copy);
                    int idx = (int)Math.Round((_count - 1) * _p);
                    return copy[Math.Clamp(idx, 0, _count - 1)];
                }
                return _q[2];
            }
        }
    }

    public sealed class RunningStatistics
    {
        private int _count;
        private double _mean;
        private double _m2;
        private double _min = double.MaxValue;
        private double _max = double.MinValue;
        private readonly object _lock = new();

        public int Count => _count;
        public double Mean => _count > 0 ? _mean : 0.0;
        public double Variance => _count > 1 ? _m2 / (_count - 1) : 0.0;
        public double StandardDeviation => Math.Sqrt(Variance);
        public double Min => _count > 0 ? _min : 0.0;
        public double Max => _count > 0 ? _max : 0.0;

        public void Push(double x)
        {
            lock (_lock)
            {
                _count++;
                if (x < _min) _min = x;
                if (x > _max) _max = x;

                var delta = x - _mean;
                _mean += delta / _count;
                var delta2 = x - _mean;
                _m2 += delta * delta2;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _count = 0;
                _mean = 0.0;
                _m2 = 0.0;
                _min = double.MaxValue;
                _max = double.MinValue;
            }
        }
    }

    public sealed class HoltLinearTrend
    {
        private readonly double _alpha;
        private readonly double _beta;
        private double _level;
        private double _trend;
        private bool _initialized;

        public HoltLinearTrend(double alpha = 0.3, double beta = 0.1)
        {
            _alpha = Math.Clamp(alpha, 0.01, 0.99);
            _beta = Math.Clamp(beta, 0.01, 0.99);
        }

        public double Update(double sample)
        {
            if (!_initialized)
            {
                _level = sample;
                _trend = 0.0;
                _initialized = true;
                return _level;
            }

            var prevLevel = _level;
            var prevTrend = _trend;

            _level = (_alpha * sample) + ((1.0 - _alpha) * (prevLevel + prevTrend));
            _trend = (_beta * (_level - prevLevel)) + ((1.0 - _beta) * prevTrend);

            return _level;
        }

        public double Forecast(int stepsAhead = 1)
        {
            if (!_initialized) return 0.0;
            return _level + (stepsAhead * _trend);
        }

        public double TrendVelocity => _trend;
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

    public static int FastBitwiseLevenshtein(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
    {
        int n = s.Length;
        int m = t.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        if (n > m)
        {
            var tempSpan = s;
            s = t;
            t = tempSpan;
            n = s.Length;
            m = t.Length;
        }

        if (n <= 64)
            return FastBitwiseLevenshtein64(s, t);

        return FastRowLevenshtein(s, t);
    }

    private static int FastBitwiseLevenshtein64(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
    {
        int n = s.Length;
        int m = t.Length;

        Span<ulong> peq = stackalloc ulong[256];
        peq.Clear();

        for (int i = 0; i < n; i++)
        {
            char c = char.ToLowerInvariant(s[i]);
            if (c < 256)
                peq[c] |= 1UL << i;
        }

        ulong pv = ~0UL;
        ulong nv = 0UL;
        int score = n;

        for (int j = 0; j < m; j++)
        {
            char tc = char.ToLowerInvariant(t[j]);
            ulong eq = tc < 256 ? peq[tc] : 0UL;

            ulong xv = eq | nv;
            ulong xh = (((eq & pv) + pv) ^ pv) | eq;

            ulong ph = nv | ~(xh | pv);
            ulong nh = pv & xh;

            if ((ph & (1UL << (n - 1))) != 0)
                score++;
            else if ((nh & (1UL << (n - 1))) != 0)
                score--;

            ph = (ph << 1) | 1UL;
            nh <<= 1;

            pv = nh | ~(xv | ph);
            nv = ph & xv;
        }

        return score;
    }

    private static int FastRowLevenshtein(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
    {
        int n = s.Length;
        int m = t.Length;

        Span<int> row = stackalloc int[n + 1];
        for (int i = 0; i <= n; i++)
            row[i] = i;

        for (int j = 1; j <= m; j++)
        {
            char tc = char.ToLowerInvariant(t[j - 1]);
            int prev = row[0];
            row[0] = j;

            for (int i = 1; i <= n; i++)
            {
                int old = row[i];
                char sc = char.ToLowerInvariant(s[i - 1]);
                int cost = sc == tc ? 0 : 1;

                row[i] = Math.Min(Math.Min(row[i] + 1, row[i - 1] + 1), prev + cost);
                prev = old;
            }
        }

        return row[n];
    }

    public static double TrigramCosineSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0.0;

        if (string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        var grams1 = ExtractTrigrams(s1.ToLowerInvariant());
        var grams2 = ExtractTrigrams(s2.ToLowerInvariant());

        if (grams1.Count == 0 || grams2.Count == 0)
            return JaroWinklerSimilarity(s1, s2);

        double dot = 0.0;
        foreach (var (gram, count1) in grams1)
        {
            if (grams2.TryGetValue(gram, out var count2))
                dot += count1 * count2;
        }

        double mag1 = 0.0;
        foreach (var c in grams1.Values) mag1 += c * c;

        double mag2 = 0.0;
        foreach (var c in grams2.Values) mag2 += c * c;

        var denominator = Math.Sqrt(mag1) * Math.Sqrt(mag2);
        return denominator > 0.0 ? dot / denominator : 0.0;
    }

    private static Dictionary<string, int> ExtractTrigrams(string str)
    {
        var dict = new Dictionary<string, int>(StringComparer.Ordinal);
        var padded = $"  {str} ";

        for (int i = 0; i <= padded.Length - 3; i++)
        {
            var tri = padded.Substring(i, 3);
            dict[tri] = dict.GetValueOrDefault(tri, 0) + 1;
        }

        return dict;
    }

    public static double JaroWinklerSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0.0;

        if (s1.Equals(s2, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        s1 = s1.ToLowerInvariant();
        s2 = s2.ToLowerInvariant();

        int len1 = s1.Length;
        int len2 = s2.Length;
        int maxDist = Math.Max(len1, len2) / 2 - 1;
        if (maxDist < 0) maxDist = 0;

        bool[] match1 = new bool[len1];
        bool[] match2 = new bool[len2];
        int matches = 0;

        for (int i = 0; i < len1; i++)
        {
            int start = Math.Max(0, i - maxDist);
            int end = Math.Min(i + maxDist + 1, len2);

            for (int j = start; j < end; j++)
            {
                if (match2[j] || s1[i] != s2[j])
                    continue;

                match1[i] = true;
                match2[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
            return 0.0;

        int transpositions = 0;
        int k = 0;
        for (int i = 0; i < len1; i++)
        {
            if (!match1[i])
                continue;

            while (!match2[k])
                k++;

            if (s1[i] != s2[k])
                transpositions++;

            k++;
        }

        double jaro = ((matches / (double)len1) +
                       (matches / (double)len2) +
                       ((matches - (transpositions / 2.0)) / matches)) / 3.0;

        int prefix = 0;
        int maxPrefix = Math.Min(4, Math.Min(len1, len2));
        for (int i = 0; i < maxPrefix; i++)
        {
            if (s1[i] == s2[i])
                prefix++;
            else
                break;
        }

        return jaro + (prefix * 0.1 * (1.0 - jaro));
    }

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

    public sealed class TokenBucket
    {
        private readonly double _capacity;
        private readonly double _refillRatePerSecond;
        private double _tokens;
        private long _lastRefillTimestamp;
        private readonly object _lock = new();

        public TokenBucket(double capacity, double refillRatePerSecond)
        {
            _capacity = Math.Max(1.0, capacity);
            _refillRatePerSecond = Math.Max(0.1, refillRatePerSecond);
            _tokens = _capacity;
            _lastRefillTimestamp = Stopwatch.GetTimestamp();
        }

        public bool TryConsume(double count = 1.0)
        {
            lock (_lock)
            {
                Refill();

                if (_tokens >= count)
                {
                    _tokens -= count;
                    return true;
                }

                return false;
            }
        }

        private void Refill()
        {
            var now = Stopwatch.GetTimestamp();
            var elapsedSeconds = (double)(now - _lastRefillTimestamp) / Stopwatch.Frequency;
            _lastRefillTimestamp = now;

            _tokens = Math.Min(_capacity, _tokens + (elapsedSeconds * _refillRatePerSecond));
        }
    }

    public sealed class HybridMemoryCache<TKey, TValue> where TKey : notnull
    {
        private readonly int _capacity;
        private readonly TimeSpan _itemTtl;
        private readonly ConcurrentDictionary<TKey, CacheNode> _map = new();
        private readonly LinkedList<TKey> _lruList = new();
        private readonly object _lock = new();

        private sealed class CacheNode
        {
            public TKey Key { get; }
            public TValue Value { get; set; }
            public DateTime ExpiresAt { get; set; }
            public int AccessFrequency { get; set; }
            public LinkedListNode<TKey>? LruNode { get; set; }

            public CacheNode(TKey key, TValue value, DateTime expiresAt)
            {
                Key = key;
                Value = value;
                ExpiresAt = expiresAt;
                AccessFrequency = 1;
            }
        }

        public HybridMemoryCache(int capacity = 500, TimeSpan? itemTtl = null)
        {
            _capacity = Math.Max(10, capacity);
            _itemTtl = itemTtl ?? TimeSpan.FromMinutes(10);
        }

        public bool TryGet(TKey key, out TValue? value)
        {
            lock (_lock)
            {
                if (_map.TryGetValue(key, out var node))
                {
                    if (DateTime.UtcNow > node.ExpiresAt)
                    {
                        if (node.LruNode != null && node.LruNode.List != null)
                        {
                            _lruList.Remove(node.LruNode);
                        }
                        _map.TryRemove(key, out _);
                        value = default;
                        return false;
                    }

                    node.AccessFrequency++;
                    if (node.LruNode != null && node.LruNode.List != null)
                    {
                        _lruList.Remove(node.LruNode);
                        _lruList.AddFirst(node.LruNode);
                    }

                    value = node.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public void Set(TKey key, TValue value)
        {
            lock (_lock)
            {
                var expires = DateTime.UtcNow + _itemTtl;

                if (_map.TryGetValue(key, out var existing))
                {
                    existing.Value = value;
                    existing.ExpiresAt = expires;
                    existing.AccessFrequency++;
                    if (existing.LruNode != null && existing.LruNode.List != null)
                    {
                        _lruList.Remove(existing.LruNode);
                        _lruList.AddFirst(existing.LruNode);
                    }
                    return;
                }

                if (_map.Count >= _capacity)
                {
                    var last = _lruList.Last;
                    if (last != null)
                    {
                        _lruList.RemoveLast();
                        _map.TryRemove(last.Value, out _);
                    }
                }

                var lruNode = new LinkedListNode<TKey>(key);
                _lruList.AddFirst(lruNode);

                var newNode = new CacheNode(key, value, expires)
                {
                    LruNode = lruNode
                };
                _map[key] = newNode;
            }
        }

        public bool Remove(TKey key)
        {
            lock (_lock)
            {
                if (_map.TryRemove(key, out var node))
                {
                    if (node.LruNode != null && node.LruNode.List != null)
                    {
                        _lruList.Remove(node.LruNode);
                    }
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _map.Clear();
                _lruList.Clear();
            }
        }

        public int Count => _map.Count;
    }

    public static int DamerauLevenshteinDistance(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
    {
        int n = s.Length;
        int m = t.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        Span<int> d0 = stackalloc int[m + 1];
        Span<int> d1 = stackalloc int[m + 1];
        Span<int> d2 = stackalloc int[m + 1];

        for (int j = 0; j <= m; j++)
            d1[j] = j;

        for (int i = 1; i <= n; i++)
        {
            d2[0] = i;
            char sc = char.ToLowerInvariant(s[i - 1]);

            for (int j = 1; j <= m; j++)
            {
                char tc = char.ToLowerInvariant(t[j - 1]);
                int cost = sc == tc ? 0 : 1;

                int del = d1[j] + 1;
                int ins = d2[j - 1] + 1;
                int sub = d1[j - 1] + cost;

                int min = Math.Min(Math.Min(del, ins), sub);

                if (i > 1 && j > 1 &&
                    char.ToLowerInvariant(s[i - 1]) == char.ToLowerInvariant(t[j - 2]) &&
                    char.ToLowerInvariant(s[i - 2]) == char.ToLowerInvariant(t[j - 1]))
                {
                    min = Math.Min(min, d0[j - 2] + cost);
                }

                d2[j] = min;
            }

            d1.CopyTo(d0);
            d2.CopyTo(d1);
        }

        return d1[m];
    }

    public static double OkapiBM25Score(
        string[] queryTerms,
        string targetDocument,
        double avgDocLength = 25.0,
        double k1 = 1.2,
        double b = 0.75)
    {
        if (queryTerms.Length == 0 || string.IsNullOrEmpty(targetDocument))
            return 0.0;

        var words = targetDocument.Split(new[] { ' ', ',', '.', '-', '_', '/', '|', ':', '[', ']', '(', ')' },
            StringSplitOptions.RemoveEmptyEntries);
        var docLen = words.Length;

        if (docLen == 0) return 0.0;

        var termFreqs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var word in words)
        {
            termFreqs[word] = termFreqs.GetValueOrDefault(word, 0) + 1;
        }

        double totalScore = 0.0;
        var lenNorm = 1.0 - b + (b * (docLen / Math.Max(1.0, avgDocLength)));

        foreach (var term in queryTerms)
        {
            if (string.IsNullOrWhiteSpace(term))
                continue;

            int tf = 0;
            foreach (var (word, count) in termFreqs)
            {
                if (word.Equals(term, StringComparison.OrdinalIgnoreCase))
                {
                    tf += count * 2;
                }
                else if (word.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                {
                    tf += count;
                }
            }

            if (tf > 0)
            {
                double idf = Math.Log(1.0 + (100.0 / (tf + 0.5)));
                double termScore = idf * (tf * (k1 + 1.0)) / (tf + (k1 * lenNorm));
                totalScore += termScore;
            }
        }

        return totalScore;
    }

    public static ulong FastHash64(ReadOnlySpan<char> text)
    {
        const ulong fnvPrime = 0x00000100000001B3UL;
        ulong hash = 0xCBF29CE484222325UL;

        for (int i = 0; i < text.Length; i++)
        {
            hash ^= (ulong)char.ToLowerInvariant(text[i]);
            hash *= fnvPrime;
        }

        hash ^= hash >> 33;
        hash *= 0xff51afd7ed558ccdUL;
        hash ^= hash >> 33;
        hash *= 0xc4ceb9fe1a85ec53UL;
        hash ^= hash >> 33;

        return hash;
    }

    public sealed class ThroughputEtaEstimator
    {
        private readonly double _smoothingAlpha;
        private double _smoothedBytesPerSecond;
        private long _lastBytes;
        private long _lastTimestamp;
        private bool _initialized;
        private readonly object _lock = new();

        public double BytesPerSecond => _smoothedBytesPerSecond;

        public ThroughputEtaEstimator(double smoothingAlpha = 0.25)
        {
            _smoothingAlpha = Math.Clamp(smoothingAlpha, 0.05, 0.95);
        }

        public TimeSpan? Update(long currentBytes, long totalBytes)
        {
            lock (_lock)
            {
                var now = Stopwatch.GetTimestamp();

                if (!_initialized)
                {
                    _lastBytes = currentBytes;
                    _lastTimestamp = now;
                    _initialized = true;
                    return null;
                }

                var elapsedSeconds = (double)(now - _lastTimestamp) / Stopwatch.Frequency;
                if (elapsedSeconds < 0.1)
                {
                    return CalculateEta(currentBytes, totalBytes);
                }

                var bytesDelta = currentBytes - _lastBytes;
                var currentRate = Math.Max(0.0, bytesDelta / elapsedSeconds);

                if (_smoothedBytesPerSecond <= 0.0)
                {
                    _smoothedBytesPerSecond = currentRate;
                }
                else
                {
                    _smoothedBytesPerSecond = (_smoothingAlpha * currentRate) +
                                              ((1.0 - _smoothingAlpha) * _smoothedBytesPerSecond);
                }

                _lastBytes = currentBytes;
                _lastTimestamp = now;

                return CalculateEta(currentBytes, totalBytes);
            }
        }

        private TimeSpan? CalculateEta(long currentBytes, long totalBytes)
        {
            if (totalBytes <= 0 || _smoothedBytesPerSecond <= 0.0)
                return null;

            var remainingBytes = totalBytes - currentBytes;
            if (remainingBytes <= 0)
                return TimeSpan.Zero;

            var remainingSeconds = remainingBytes / _smoothedBytesPerSecond;
            if (double.IsNaN(remainingSeconds) || double.IsInfinity(remainingSeconds) || remainingSeconds > 86400)
                return null;

            return TimeSpan.FromSeconds(remainingSeconds);
        }
    }

    /// <summary>
    /// Myers bit-parallel edit distance algorithm (Fast O(N) bitwise Levenshtein for strings &lt;= 64 chars).
    /// Zero heap allocations, pure CPU register computation.
    /// </summary>
    public static int MyersBitParallelDistance(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
    {
        int m = pattern.Length;
        int n = text.Length;

        if (m == 0) return n;
        if (n == 0) return m;

        // If pattern is longer than 64 chars, fallback to standard DamerauLevenshtein
        if (m > 64)
        {
            if (n <= 64)
            {
                return MyersBitParallelDistance(text, pattern);
            }
            return DamerauLevenshteinDistance(pattern, text);
        }

        // Build pattern bitmask table
        Span<ulong> peq = stackalloc ulong[128];
        peq.Clear();
        Dictionary<char, ulong>? peqExtended = null;

        for (int i = 0; i < m; i++)
        {
            char c = char.ToLowerInvariant(pattern[i]);
            ulong bit = 1UL << i;
            if (c < 128)
            {
                peq[c] |= bit;
            }
            else
            {
                peqExtended ??= new Dictionary<char, ulong>();
                if (peqExtended.TryGetValue(c, out var mask))
                    peqExtended[c] = mask | bit;
                else
                    peqExtended[c] = bit;
            }
        }

        ulong pv = ~0UL;
        ulong mv = 0UL;
        int score = m;

        for (int j = 0; j < n; j++)
        {
            char c = char.ToLowerInvariant(text[j]);
            ulong eq;
            if (c < 128)
            {
                eq = peq[c];
            }
            else if (peqExtended != null && peqExtended.TryGetValue(c, out var mask))
            {
                eq = mask;
            }
            else
            {
                eq = 0UL;
            }

            ulong xv = eq | mv;
            ulong xh = (((eq & pv) + pv) ^ pv) | eq;

            ulong ph = mv | ~(xh | pv);
            ulong mh = pv & xh;

            if ((ph & (1UL << (m - 1))) != 0)
                score++;
            else if ((mh & (1UL << (m - 1))) != 0)
                score--;

            ph <<= 1;
            mh <<= 1;

            pv = mh | ~(xv | ph);
            mv = ph & xv;
        }

        return score;
    }

    /// <summary>
    /// Ultra-fast memory-efficient prefix &amp; token search index for fast server list querying.
    /// </summary>
    public sealed class FastServerSearchIndex<T>
    {
        private readonly ConcurrentDictionary<string, HashSet<T>> _tokenMap = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<(string Name, T Item)> _allEntries = new();
        private readonly object _lock = new();

        public void Rebuild(IEnumerable<(string Name, T Item)> entries)
        {
            lock (_lock)
            {
                _tokenMap.Clear();
                _allEntries.Clear();

                foreach (var (name, item) in entries)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    _allEntries.Add((name, item));
                    var tokens = name.Split([' ', '-', '_', '/', '|', ':', '[', ']', '(', ')', '.', ','], StringSplitOptions.RemoveEmptyEntries);

                    foreach (var token in tokens)
                    {
                        var t = token.Trim().ToLowerInvariant();
                        if (t.Length == 0) continue;

                        _tokenMap.AddOrUpdate(t,
                            _ => new HashSet<T> { item },
                            (_, set) => { lock (set) { set.Add(item); return set; } });
                    }
                }
            }
        }

        public List<T> Search(string query, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                lock (_lock)
                {
                    return _allEntries.Select(x => x.Item).Take(maxResults).ToList();
                }
            }

            var q = query.Trim().ToLowerInvariant();
            var results = new HashSet<T>();

            // 1. Direct token lookup
            foreach (var kvp in _tokenMap)
            {
                if (kvp.Key.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                {
                    lock (kvp.Value)
                    {
                        results.UnionWith(kvp.Value);
                    }
                    if (results.Count >= maxResults) break;
                }
            }

            // 2. Substring scan if not enough results
            if (results.Count < maxResults)
            {
                lock (_lock)
                {
                    foreach (var (name, item) in _allEntries)
                    {
                        if (results.Contains(item)) continue;

                        if (name.Contains(q, StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(item);
                            if (results.Count >= maxResults) break;
                        }
                    }
                }
            }

            return results.Take(maxResults).ToList();
        }
    }

    /// <summary>
    /// Hardware-accelerated SIMD string processing (Vector128 / Vector256 / AVX2 / SSE2 / AdvSIMD).
    /// </summary>
    public static class SimdStringHelper
    {
        public static void ToLowerAsciiSimd(ReadOnlySpan<char> source, Span<char> destination, bool forceScalar = false)
        {
            if (destination.Length < source.Length)
                throw new ArgumentException("Destination span is too short");

            int i = 0;
            int length = source.Length;
            var srcSpanUshort = System.Runtime.InteropServices.MemoryMarshal.Cast<char, ushort>(source);
            var dstSpanUshort = System.Runtime.InteropServices.MemoryMarshal.Cast<char, ushort>(destination);

            if (!forceScalar && System.Runtime.Intrinsics.Vector256.IsHardwareAccelerated && length >= 16)
            {
                var lowerA = System.Runtime.Intrinsics.Vector256.Create((ushort)'A');
                var upperZ = System.Runtime.Intrinsics.Vector256.Create((ushort)'Z');
                var diff = System.Runtime.Intrinsics.Vector256.Create((ushort)('a' - 'A'));

                while (i <= length - 16)
                {
                    var vec = System.Runtime.Intrinsics.Vector256.Create(srcSpanUshort.Slice(i, 16));
                    var maskGe = System.Runtime.Intrinsics.Vector256.GreaterThanOrEqual(vec, lowerA);
                    var maskLe = System.Runtime.Intrinsics.Vector256.LessThanOrEqual(vec, upperZ);
                    var isUpper = maskGe & maskLe;
                    var offset = isUpper & diff;
                    var result = vec + offset;

                    System.Runtime.CompilerServices.Unsafe.As<ushort, System.Runtime.Intrinsics.Vector256<ushort>>(ref dstSpanUshort[i]) = result;
                    i += 16;
                }
            }
            else if (!forceScalar && System.Runtime.Intrinsics.Vector128.IsHardwareAccelerated && length >= 8)
            {
                var lowerA = System.Runtime.Intrinsics.Vector128.Create((ushort)'A');
                var upperZ = System.Runtime.Intrinsics.Vector128.Create((ushort)'Z');
                var diff = System.Runtime.Intrinsics.Vector128.Create((ushort)('a' - 'A'));

                while (i <= length - 8)
                {
                    var vec = System.Runtime.Intrinsics.Vector128.Create(srcSpanUshort.Slice(i, 8));
                    var maskGe = System.Runtime.Intrinsics.Vector128.GreaterThanOrEqual(vec, lowerA);
                    var maskLe = System.Runtime.Intrinsics.Vector128.LessThanOrEqual(vec, upperZ);
                    var isUpper = maskGe & maskLe;
                    var offset = isUpper & diff;
                    var result = vec + offset;

                    System.Runtime.CompilerServices.Unsafe.As<ushort, System.Runtime.Intrinsics.Vector128<ushort>>(ref dstSpanUshort[i]) = result;
                    i += 8;
                }
            }

            for (; i < length; i++)
            {
                char c = source[i];
                destination[i] = (c >= 'A' && c <= 'Z') ? (char)(c + 32) : c;
            }
        }
    }
}
