using TapBpm.Core;

namespace TapBpm.Tests;

public class TapTempoTests
{
    /// <summary>A clock the test drives by hand, so timing is exact instead of wall-clock dependent.</summary>
    private sealed class FakeClock
    {
        public double NowMs { get; private set; }
        public void Advance(double ms) => NowMs += ms;
    }

    private static (TapTempo Tempo, FakeClock Clock) Create()
    {
        var clock = new FakeClock();
        return (new TapTempo(() => clock.NowMs), clock);
    }

    /// <summary>
    /// Taps at a fixed interval, starting at the current time and leaving the clock sitting
    /// on the final tap, so a following Advance() reads as the gap after it.
    /// </summary>
    private static void TapEvenly(TapTempo tempo, FakeClock clock, int taps, double intervalMs)
    {
        for (int i = 0; i < taps; i++)
        {
            if (i > 0)
                clock.Advance(intervalMs);
            tempo.Tap();
        }
    }

    [Fact]
    public void No_tempo_before_the_second_tap()
    {
        var (tempo, _) = Create();

        tempo.Tap();

        Assert.Null(tempo.Bpm);
        Assert.Equal(1, tempo.TapCount);
    }

    [Theory]
    [InlineData(500, 120)]
    [InlineData(400, 150)]
    [InlineData(1000, 60)]
    [InlineData(300, 200)]
    public void Even_taps_give_the_exact_tempo(double intervalMs, double expectedBpm)
    {
        var (tempo, clock) = Create();

        TapEvenly(tempo, clock, taps: 8, intervalMs);

        Assert.NotNull(tempo.Bpm);
        Assert.Equal(expectedBpm, tempo.Bpm!.Value, precision: 6);
    }

    [Fact]
    public void Reset_clears_the_last_tap_time_too()
    {
        // The original kept the previous tap time across a reset, so the first tap
        // afterwards was measured against a tap from before it.
        var (tempo, clock) = Create();
        TapEvenly(tempo, clock, taps: 6, intervalMs: 500);

        tempo.Reset();
        Assert.Null(tempo.Bpm);
        Assert.Equal(0, tempo.TapCount);

        clock.Advance(5_000);          // a long silence, as if the user walked away
        tempo.Tap();

        Assert.Null(tempo.Bpm);        // one tap cannot imply a tempo
        Assert.Equal(1, tempo.TapCount);
    }

    [Fact]
    public void A_long_gap_starts_a_new_measurement()
    {
        var (tempo, clock) = Create();
        TapEvenly(tempo, clock, taps: 6, intervalMs: 500);

        clock.Advance(4_000);
        Assert.Equal(TapTempo.TapResult.Restarted, tempo.Tap());
        Assert.Null(tempo.Bpm);

        clock.Advance(400);
        TapEvenly(tempo, clock, taps: 7, intervalMs: 400);

        Assert.Equal(150, tempo.Bpm!.Value, precision: 6);
    }

    [Fact]
    public void Taps_that_are_too_close_together_are_ignored()
    {
        var (tempo, clock) = Create();
        TapEvenly(tempo, clock, taps: 5, intervalMs: 500);
        int before = tempo.TapCount;

        clock.Advance(40);             // a key repeat or a double strike
        Assert.Equal(TapTempo.TapResult.Ignored, tempo.Tap());
        Assert.Equal(before, tempo.TapCount);
    }

    [Fact]
    public void One_stray_tap_does_not_move_the_tempo()
    {
        var (tempo, clock) = Create();
        TapEvenly(tempo, clock, taps: 8, intervalMs: 500);
        double steady = tempo.Bpm!.Value;

        clock.Advance(250);            // a fumbled tap, half a beat early
        tempo.Tap();

        Assert.Equal(steady, tempo.Bpm!.Value, precision: 6);
    }

    [Fact]
    public void Two_stray_taps_in_a_row_are_treated_as_a_new_tempo()
    {
        var (tempo, clock) = Create();
        TapEvenly(tempo, clock, taps: 8, intervalMs: 500);

        clock.Advance(250);
        tempo.Tap();                   // first outlier: dropped
        clock.Advance(250);
        Assert.Equal(TapTempo.TapResult.Restarted, tempo.Tap());

        clock.Advance(250);
        TapEvenly(tempo, clock, taps: 8, intervalMs: 250);

        Assert.Equal(240, tempo.Bpm!.Value, precision: 6);
    }

    [Fact]
    public void Jitter_averages_out_instead_of_accumulating()
    {
        // Taps alternating either side of a 500 ms beat should still read as 120 BPM.
        var (tempo, clock) = Create();
        double[] intervals = { 470, 530, 480, 520, 495, 505, 510, 490 };

        tempo.Tap();
        foreach (double interval in intervals)
        {
            clock.Advance(interval);
            tempo.Tap();
        }

        Assert.InRange(tempo.Bpm!.Value, 119, 121);
    }

    [Fact]
    public void Only_the_recent_window_counts()
    {
        // A sloppy start must not weigh on the reading forever: after a full window of
        // steady taps the early ones have aged out entirely.
        var (tempo, clock) = Create();

        tempo.Tap();
        clock.Advance(900);            // deliberately off-tempo opening
        tempo.Tap();
        clock.Advance(500);

        TapEvenly(tempo, clock, taps: 20, intervalMs: 500);

        Assert.Equal(120, tempo.Bpm!.Value, precision: 6);
    }

    [Theory]
    [InlineData(0.5, 60)]
    [InlineData(2.0, 240)]
    public void Halving_and_doubling_scale_the_tempo(double factor, double expected)
    {
        var (tempo, clock) = Create();
        TapEvenly(tempo, clock, taps: 8, intervalMs: 500);

        tempo.Scale(factor);

        Assert.Equal(expected, tempo.Bpm!.Value, precision: 6);
    }

    [Fact]
    public void Further_taps_refine_the_scaled_tempo_rather_than_undoing_it()
    {
        var (tempo, clock) = Create();
        TapEvenly(tempo, clock, taps: 8, intervalMs: 500);
        tempo.Scale(2.0);              // 240 BPM, i.e. a 250 ms beat

        clock.Advance(250);
        TapEvenly(tempo, clock, taps: 6, intervalMs: 250);

        Assert.Equal(240, tempo.Bpm!.Value, precision: 6);
    }

    [Fact]
    public void Scaling_out_of_range_is_refused()
    {
        var (tempo, clock) = Create();
        TapEvenly(tempo, clock, taps: 8, intervalMs: 500);   // 120 BPM

        tempo.Scale(2.0);              // 240
        tempo.Scale(2.0);              // 480
        tempo.Scale(2.0);              // 960
        tempo.Scale(2.0);              // 1920 would be past the ceiling, so it is ignored

        Assert.Equal(960, tempo.Bpm!.Value, precision: 6);
    }

    [Fact]
    public void Steady_tapping_reads_as_stable_and_sloppy_tapping_does_not()
    {
        var (steadyTempo, steadyClock) = Create();
        TapEvenly(steadyTempo, steadyClock, taps: 10, intervalMs: 500);

        var (sloppyTempo, sloppyClock) = Create();
        double[] intervals = { 430, 560, 450, 545, 470, 540, 455, 550 };
        sloppyTempo.Tap();
        foreach (double interval in intervals)
        {
            sloppyClock.Advance(interval);
            sloppyTempo.Tap();
        }

        Assert.Equal(1.0, steadyTempo.Stability!.Value, precision: 6);
        Assert.True(sloppyTempo.Stability < 0.5,
            $"expected sloppy tapping to score low, got {sloppyTempo.Stability}");
    }

    [Fact]
    public void Stability_is_unknown_until_there_are_enough_taps()
    {
        var (tempo, clock) = Create();
        TapEvenly(tempo, clock, taps: 3, intervalMs: 500);

        Assert.NotNull(tempo.Bpm);
        Assert.Null(tempo.Stability);
    }
}
