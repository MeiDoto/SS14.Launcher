using System;
using System.Diagnostics;

namespace SS14.Launcher.Utility.Algorithms.Network;

/// <summary>
/// Exponentially smoothed throughput rate and ETA estimator for downloads and updates.
/// </summary>
public class ThroughputEtaEstimator
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
