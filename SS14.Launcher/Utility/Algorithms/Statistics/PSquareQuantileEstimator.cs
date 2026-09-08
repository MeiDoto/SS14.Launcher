using System;

namespace SS14.Launcher.Utility.Algorithms.Statistics;

/// <summary>
/// Piecewise-parabolic P-Square algorithm for dynamic, dynamic-sample quantile tracking without storing all samples.
/// </summary>
public class PSquareQuantileEstimator
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
