using NAudio.Wave;

namespace TapBpm.Core;

/// <summary>
/// Plays a click at the detected tempo so you can check the reading by ear.
/// </summary>
/// <remarks>
/// The click is placed at an exact sample offset inside a continuously running audio
/// stream rather than triggered by a timer, so it does not drift or jitter no matter what
/// the UI thread is doing. Changing the tempo keeps the current beat phase, so the click
/// speeds up or slows down without stuttering.
/// </remarks>
public sealed class Metronome : IDisposable
{
    private readonly ClickProvider _provider = new();
    private WaveOutEvent? _output;
    private bool _disposed;

    public bool IsRunning { get; private set; }

    /// <summary>Tempo in BPM. Ignored when out of a sensible range.</summary>
    public double Bpm
    {
        get => _provider.Bpm;
        set { if (value is >= 20 and <= 999) _provider.Bpm = value; }
    }

    public void Start()
    {
        if (IsRunning || _disposed)
            return;

        _output = new WaveOutEvent { DesiredLatency = 80, NumberOfBuffers = 3 };
        _output.Init(_provider);
        _provider.ResetPhase();
        _output.Play();
        IsRunning = true;
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        _output?.Stop();
        _output?.Dispose();
        _output = null;
        IsRunning = false;
    }

    public void Toggle()
    {
        if (IsRunning) Stop(); else Start();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Stop();
    }

    private sealed class ClickProvider : ISampleProvider
    {
        private const int SampleRate = 44100;
        private const double ClickFrequency = 1400;   // short, dry, cuts through a mix
        private const double ClickSeconds = 0.022;
        private const float Amplitude = 0.32f;

        private readonly object _gate = new();
        private double _bpm = 120;
        private double _phase;                        // 0 to 1 within the current beat

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1);

        public double Bpm
        {
            get { lock (_gate) return _bpm; }
            set { lock (_gate) _bpm = value; }
        }

        public void ResetPhase()
        {
            lock (_gate) _phase = 0;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            double bpm, phase;
            lock (_gate) { bpm = _bpm; phase = _phase; }

            double samplesPerBeat = SampleRate * 60.0 / bpm;
            double phaseStep = 1.0 / samplesPerBeat;
            double clickSamples = ClickSeconds * SampleRate;

            for (int i = 0; i < count; i++)
            {
                double samplesIntoBeat = phase * samplesPerBeat;
                float value = 0;

                if (samplesIntoBeat < clickSamples)
                {
                    // Exponential decay keeps the click tight instead of ringing.
                    double envelope = Math.Exp(-5.0 * (samplesIntoBeat / clickSamples));
                    value = (float)(Math.Sin(2 * Math.PI * ClickFrequency * samplesIntoBeat / SampleRate)
                                    * envelope * Amplitude);
                }

                buffer[offset + i] = value;

                phase += phaseStep;
                if (phase >= 1.0)
                    phase -= 1.0;
            }

            lock (_gate) _phase = phase;
            return count;
        }
    }
}
