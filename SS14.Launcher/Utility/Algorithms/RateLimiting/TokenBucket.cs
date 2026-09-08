using System;
using System.Diagnostics;

namespace SS14.Launcher.Utility.Algorithms.RateLimiting;

/// <summary>
/// Thread-safe Token Bucket rate limiter for network operations and hub requests.
/// </summary>
public class TokenBucket
{
    private readonly double _capacity;
    private readonly double _refillRatePerSecond;
    private double _tokens;
    private long _lastRefillTimestamp;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new token bucket rate limiter.
    /// </summary>
    /// <param name="capacity">Maximum number of tokens the bucket can hold.</param>
    /// <param name="refillRatePerSecond">Rate at which tokens are replenished per second.</param>
    public TokenBucket(double capacity, double refillRatePerSecond)
    {
        _capacity = Math.Max(1.0, capacity);
        _refillRatePerSecond = Math.Max(0.1, refillRatePerSecond);
        _tokens = _capacity;
        _lastRefillTimestamp = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Attempts to consume tokens from the bucket. Thread-safe.
    /// </summary>
    /// <param name="count">Number of tokens to consume. Default: 1.</param>
    /// <returns><c>true</c> if sufficient tokens were available; <c>false</c> if the request was rate-limited.</returns>
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
