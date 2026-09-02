using System;

namespace SS14.Launcher.Utility.Algorithms.Statistics;

/// <summary>
/// Holt's double exponential smoothing (level + trend linear forecasting).
/// </summary>
public class HoltLinearTrend
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
