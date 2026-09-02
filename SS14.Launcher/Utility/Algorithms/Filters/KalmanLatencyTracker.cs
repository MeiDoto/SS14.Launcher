using System;

namespace SS14.Launcher.Utility.Algorithms.Filters;

/// <summary>
/// 1D Kalman filter tracker with Chi-squared gating to filter latency spikes and estimate packet jitter.
/// </summary>
public class KalmanLatencyTracker
{
    private float _estimate;
    private float _errorCovariance = 100.0f;
    private readonly float _processNoiseQ;
    private float _measurementNoiseR = 25.0f;
    private float _jitterEma;
    private bool _initialized;
    private readonly object _lock = new();

    /// <summary>
    /// Gets the current smoothed ping estimate in milliseconds.
    /// </summary>
    public float EstimatedPing => _estimate;

    /// <summary>
    /// Gets the current exponentially smoothed jitter estimate in milliseconds.
    /// </summary>
    public float EstimatedJitter => _jitterEma;

    /// <summary>
    /// Initializes a new Kalman latency tracker.
    /// </summary>
    /// <param name="processNoiseQ">
    /// Process noise covariance (Q). Higher values make the filter more responsive to changes
    /// but less smooth. Typical range: 0.1–2.0. Default: 0.5.
    /// </param>
    public KalmanLatencyTracker(float processNoiseQ = 0.5f)
    {
        _processNoiseQ = MathF.Max(0.01f, processNoiseQ);
    }

    /// <summary>
    /// Feeds a new latency measurement into the Kalman filter and returns updated estimates.
    /// Thread-safe. Extreme outliers are clamped via Chi-squared gating (3.5σ threshold).
    /// </summary>
    /// <param name="measurement">Raw ping measurement in milliseconds.</param>
    /// <returns>Tuple of (smoothed ping, estimated jitter) in milliseconds.</returns>
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
