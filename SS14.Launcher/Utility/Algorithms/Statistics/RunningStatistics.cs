using System;

namespace SS14.Launcher.Utility.Algorithms.Statistics;

/// <summary>
/// Incremental online sample statistics computation using Welford's algorithm.
/// </summary>
public class RunningStatistics
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
