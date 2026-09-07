using System.Diagnostics;

namespace TapBpm.Core;

/// <summary>
/// Turns a stream of taps into a tempo.
/// </summary>
/// <remarks>
/// Two things the original version got wrong are fixed here. Timing comes from a
/// <see cref="Stopwatch"/> rather than <c>DateTime.Now</c>, whose ~15 ms granularity is
/// coarse enough to wobble the reading by several BPM; and the tempo is estimated over a
/// sliding window instead of every tap since launch, so a sloppy start stops poisoning
/// the result forever and the number settles as you keep tapping.
/// </remarks>
public sealed class TapTempo
{
    /// <summary>Taps kept for the estimate. Long enough to average out human jitter, short enough to follow a drifting tempo.</summary>
    private const int WindowSize = 16;

    /// <summary>A gap longer than this means the user stopped and is timing something new.</summary>
    private const double ResetGapMs = 2500;

    /// <summary>Taps closer together than this are a key repeat or a double-strike, not a beat (≈ 480 BPM).</summary>
    private const double MinIntervalMs = 125;

    /// <summary>How far an interval may stray from the running median before it is treated as a misfire.</summary>
    private const double OutlierLowRatio = 0.62;
    private const double OutlierHighRatio = 1.62;

    private readonly Func<double> _nowMs;
    private readonly List<double> _taps = new();   // tap timestamps in ms

    /// <summary>The last tap the user made, accepted or not. Rejected taps still mark time.</summary>
    private double? _lastObserved;
    private int _consecutiveOutliers;

    public TapTempo() : this(CreateStopwatchClock()) { }

    /// <summary>Test seam: lets a suite feed exact timestamps instead of tapping in real time.</summary>
    public TapTempo(Func<double> clockMs) => _nowMs = clockMs;

    private static Func<double> CreateStopwatchClock()
    {
        var clock = Stopwatch.StartNew();
        return () => clock.Elapsed.TotalMilliseconds;
    }

    /// <summary>Detected tempo, or null while there are not yet two usable taps.</summary>
    public double? Bpm { get; private set; }

    /// <summary>Taps currently contributing to the estimate.</summary>
    public int TapCount => _taps.Count;

    /// <summary>0 to 1, how consistent the recent taps are. Null until there is enough data to judge.</summary>
    public double? Stability { get; private set; }

    /// <summary>Milliseconds since the last accepted tap, for animating in time with the beat.</summary>
    public double MsSinceLastTap => _taps.Count == 0 ? double.MaxValue : _nowMs() - _taps[^1];

    public enum TapResult { Accepted, Ignored, Restarted }

    public TapResult Tap()
    {
        double now = _nowMs();

        if (_lastObserved is double previous)
        {
            double gap = now - previous;

            if (gap < MinIntervalMs)
                return TapResult.Ignored;   // key repeat or double strike; not a beat

            if (gap > ResetGapMs)
            {
                _lastObserved = now;
                Restart(now);
                return TapResult.Restarted;
            }

            // Gaps are measured against the previous tap the user actually made, including
            // ones that were rejected. Comparing against the last *accepted* tap instead
            // would make a genuine tempo change look normal every other tap, so the app
            // would never catch up with it.
            if (_taps.Count >= 3)
            {
                double median = MedianInterval();
                if (gap < median * OutlierLowRatio || gap > median * OutlierHighRatio)
                {
                    _lastObserved = now;

                    // One wild interval is usually a fumbled or missed tap, so it is simply
                    // dropped. Two in a row means the tempo really did change, and those two
                    // taps are exactly the evidence for the new one.
                    if (++_consecutiveOutliers >= 2)
                    {
                        Restart(previous);
                        _taps.Add(now);
                        Recalculate();
                        return TapResult.Restarted;
                    }

                    return TapResult.Ignored;
                }
            }
        }

        _consecutiveOutliers = 0;
        _lastObserved = now;
        _taps.Add(now);
        if (_taps.Count > WindowSize)
            _taps.RemoveAt(0);

        Recalculate();
        return TapResult.Accepted;
    }

    public void Reset()
    {
        // The original cleared the interval list but kept the last tap time, so the first
        // tap after a reset was measured against a tap from before it.
        _taps.Clear();
        _lastObserved = null;
        _consecutiveOutliers = 0;
        Bpm = null;
        Stability = null;
    }

    /// <summary>Halves or doubles the tempo, for when the taps landed on the wrong beat division.</summary>
    public void Scale(double factor)
    {
        if (Bpm is not double bpm)
            return;

        double scaled = bpm * factor;
        if (scaled is < 20 or > 999)
            return;

        Bpm = scaled;

        // Re-space the stored taps so further taps refine the scaled tempo rather than
        // fighting it back to the old value.
        if (_taps.Count >= 2)
        {
            double last = _taps[^1];
            double period = 60000.0 / scaled;
            for (int i = 0; i < _taps.Count; i++)
                _taps[i] = last - period * (_taps.Count - 1 - i);
        }
    }

    private void Restart(double at)
    {
        _taps.Clear();
        _consecutiveOutliers = 0;
        Bpm = null;
        Stability = null;
        _taps.Add(at);
    }

    private double MedianInterval()
    {
        var intervals = Intervals();
        intervals.Sort();
        int mid = intervals.Count / 2;
        return intervals.Count % 2 == 0
            ? (intervals[mid - 1] + intervals[mid]) / 2
            : intervals[mid];
    }

    private List<double> Intervals()
    {
        var intervals = new List<double>(_taps.Count - 1);
        for (int i = 1; i < _taps.Count; i++)
            intervals.Add(_taps[i] - _taps[i - 1]);
        return intervals;
    }

    private void Recalculate()
    {
        if (_taps.Count < 2)
        {
            Bpm = null;
            Stability = null;
            return;
        }

        // Least-squares fit of timestamp against beat number. Averaging the intervals would
        // reduce to (last - first) / n, which throws away every tap in between; the fit uses
        // all of them, so one shaky tap barely moves the result.
        int n = _taps.Count;
        double meanIndex = (n - 1) / 2.0;
        double meanTime = _taps.Average();

        double covariance = 0, variance = 0;
        for (int i = 0; i < n; i++)
        {
            double di = i - meanIndex;
            covariance += di * (_taps[i] - meanTime);
            variance += di * di;
        }

        double periodMs = covariance / variance;   // variance > 0 whenever n >= 2
        if (periodMs <= 0)
        {
            Bpm = null;
            Stability = null;
            return;
        }

        Bpm = Math.Clamp(60000.0 / periodMs, 20, 999);
        Stability = ComputeStability(periodMs);
    }

    private double? ComputeStability(double periodMs)
    {
        if (_taps.Count < 4)
            return null;

        var intervals = Intervals();
        double mean = intervals.Average();
        double sumSquares = intervals.Sum(v => (v - mean) * (v - mean));
        double deviation = Math.Sqrt(sumSquares / intervals.Count);

        // Roughly: within 1% of the period reads as rock solid, 10% off reads as unusable.
        double relative = deviation / periodMs;
        return Math.Clamp(1 - (relative - 0.01) / 0.09, 0, 1);
    }
}
