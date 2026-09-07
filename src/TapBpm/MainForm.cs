using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using TapBpm.Core;
using TapBpm.Ui;

namespace TapBpm;

public sealed class MainForm : Form
{
    // Layout is written in logical units at 96 DPI and multiplied through Px() at runtime,
    // so the window is laid out identically at 100%, 150% or 200% scaling.
    private const int BaseWidth = 330;
    private const int BaseHeight = 230;
    private const int Pad = 14;
    private const int ButtonSize = 28;
    private const int ButtonGap = 6;
    private const int LogoTop = 16;
    private const int LogoWidth = 176;
    private const int LogoHeight = 65;
    private const int NumberTop = 84;
    private const int NumberHeight = 100;
    private const int MeterTop = 188;
    private const int FooterTop = 196;
    private const int FooterHeight = 24;
    private const int CornerRadius = 12;

    private const string SpotifyUrl = "https://open.spotify.com/intl-pt/artist/63F0KeKFXQd5S4b3BKBfAI";

    private readonly AppSettings _settings;
    private readonly TapTempo _tempo = new();
    private readonly Metronome _metronome = new();
    private readonly GlobalHotkey _hotkey = new(GlobalHotkey.Modifiers.Control | GlobalHotkey.Modifiers.Alt, Keys.Space);

    /// <summary>Tried in order; the first one Windows will give us wins.</summary>
    private static readonly (GlobalHotkey.Modifiers Modifiers, Keys Key)[] HotkeyCandidates =
    {
        (GlobalHotkey.Modifiers.Control | GlobalHotkey.Modifiers.Alt, Keys.Space),
        (GlobalHotkey.Modifiers.Control | GlobalHotkey.Modifiers.Shift, Keys.Space),
        (GlobalHotkey.Modifiers.Control | GlobalHotkey.Modifiers.Alt, Keys.B),
        (GlobalHotkey.Modifiers.Control | GlobalHotkey.Modifiers.Shift, Keys.B),
        (GlobalHotkey.Modifiers.Alt | GlobalHotkey.Modifiers.Shift, Keys.T),
    };

    private readonly PictureBox _logo = new();
    private readonly Image? _logoSource = LoadLogo();
    private readonly IconButton _closeButton = new(Glyph.Close) { DangerOnHover = true };
    private readonly IconButton _resetButton = new(Glyph.Reset);
    private readonly IconButton _pinButton = new(Glyph.Pin);
    private readonly IconButton _metronomeButton = new(Glyph.Metronome);
    private readonly PillButton _halveButton = new("1/2");
    private readonly PillButton _doubleButton = new("x2");

    private readonly System.Windows.Forms.Timer _animation = new() { Interval = 16 };
    private readonly ToolTip _tips = new() { InitialDelay = 450, ReshowDelay = 200 };

    private Color _background;
    private Color _ink;
    private Color _mutedInk;
    private Font _numberFont = null!;
    private Font _decimalFont = null!;
    private Font _unitFont = null!;
    private Font _footerFont = null!;
    private Font _pillFont = null!;
    private double _pulse;              // 1 right after a tap, decaying to 0
    private bool _hotkeyUnavailable;

    public MainForm()
    {
        _settings = AppSettings.Load();

        SuspendLayout();
        InitializeWindow();
        InitializeControls();
        ResumeLayout(false);

        ApplyPalette(Theme.RandomBackground());
        ApplySettings();
        WireEvents();
    }

    // -- setup ------------------------------------------------------------------------

    private void InitializeWindow()
    {
        Text = "Tap BPM";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.None;   // handled explicitly in ApplyLayout
        DoubleBuffered = true;
        KeyPreview = true;
        ShowInTaskbar = true;
        ClientSize = new Size(Px(BaseWidth), Px(BaseHeight));

        // The original never assigned this, so the taskbar and Alt+Tab fell back to the
        // stock WinForms icon even though the .exe itself had one.
        Icon = LoadAppIcon();

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer, true);
    }

    private void InitializeControls()
    {
        _logo.SizeMode = PictureBoxSizeMode.Zoom;
        _logo.BackColor = Color.Transparent;
        _logo.Cursor = Cursors.Hand;

        Controls.AddRange(new Control[]
        {
            _logo, _closeButton, _resetButton, _pinButton, _metronomeButton, _halveButton, _doubleButton,
        });

        _tips.SetToolTip(_logo, "Vipz on Spotify");
        _tips.SetToolTip(_closeButton, "Close  (Esc)");
        _tips.SetToolTip(_resetButton, "Reset  (R)");
        _tips.SetToolTip(_pinButton, "Always on top  (T)");
        _tips.SetToolTip(_metronomeButton, "Play the tempo  (M)");
        _tips.SetToolTip(_halveButton, "Halve the tempo");
        _tips.SetToolTip(_doubleButton, "Double the tempo");

        ContextMenuStrip = BuildContextMenu();
        UpdateTempoButtons();
        ApplyLayout();
    }

    private void WireEvents()
    {
        _closeButton.Click += (_, _) => Close();
        _resetButton.Click += (_, _) => ResetTempo();
        _pinButton.Click += (_, _) => SetAlwaysOnTop(!TopMost);
        _metronomeButton.Click += (_, _) => ToggleMetronome();
        _halveButton.Click += (_, _) => ScaleTempo(0.5);
        _doubleButton.Click += (_, _) => ScaleTempo(2.0);
        _logo.Click += (_, _) => OpenSpotify();

        _animation.Tick += (_, _) => AdvanceAnimation();
        _hotkey.Pressed += (_, _) => Tap();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip { ShowImageMargin = false };

        var onTop = new ToolStripMenuItem("Always on top", null, (_, _) => SetAlwaysOnTop(!TopMost))
        { ShortcutKeyDisplayString = "T" };
        var metronome = new ToolStripMenuItem("Metronome", null, (_, _) => ToggleMetronome())
        { ShortcutKeyDisplayString = "M" };
        var hotkey = new ToolStripMenuItem("Global hotkey", null, (_, _) => SetGlobalHotkey(!_settings.GlobalHotkeyEnabled))
        { ShortcutKeyDisplayString = "Ctrl+Alt+Space" };   // updated in Opening to whatever was granted

        menu.Opening += (_, _) =>
        {
            onTop.Checked = TopMost;
            metronome.Checked = _metronome.IsRunning;
            hotkey.Checked = _settings.GlobalHotkeyEnabled && _hotkey.IsRegistered;
            hotkey.ShortcutKeyDisplayString = _hotkey.Describe();
            hotkey.ToolTipText = _hotkeyUnavailable
                ? "Every candidate shortcut is already owned by another application"
                : null;
        };

        menu.Items.AddRange(new ToolStripItem[]
        {
            onTop,
            metronome,
            hotkey,
            new ToolStripSeparator(),
            new ToolStripMenuItem("Copy BPM", null, (_, _) => CopyBpm()) { ShortcutKeyDisplayString = "Ctrl+C" },
            new ToolStripMenuItem("New colour", null, (_, _) => ApplyPalette(NextBackground())),
            new ToolStripMenuItem("Reset", null, (_, _) => ResetTempo()) { ShortcutKeyDisplayString = "R" },
            new ToolStripSeparator(),
            new ToolStripMenuItem("Exit", null, (_, _) => Close()),
        });

        return menu;
    }

    private void ApplySettings()
    {
        SetAlwaysOnTop(_settings.AlwaysOnTop);
        SetGlobalHotkey(_settings.GlobalHotkeyEnabled);

        if (_settings.HasSavedPosition)
        {
            var saved = new Point(_settings.WindowX, _settings.WindowY);
            // Only restore it if that spot is still on a connected monitor.
            if (Screen.AllScreens.Any(s => s.WorkingArea.Contains(saved)))
            {
                StartPosition = FormStartPosition.Manual;
                Location = saved;
            }
        }
    }

    // -- layout -----------------------------------------------------------------------

    /// <summary>Converts a logical (96 DPI) length into device pixels.</summary>
    private int Px(int logical) => (int)Math.Round(logical * DeviceDpi / 96.0);

    private void ApplyLayout()
    {
        ClientSize = new Size(Px(BaseWidth), Px(BaseHeight));

        _logo.SetBounds(Px(Pad - 2), Px(LogoTop), Px(LogoWidth), Px(LogoHeight));

        int buttonX = Px(BaseWidth - Pad - ButtonSize);
        int size = Px(ButtonSize);
        int step = Px(ButtonSize + ButtonGap);
        IconButton[] column = { _closeButton, _resetButton, _pinButton, _metronomeButton };
        for (int i = 0; i < column.Length; i++)
            column[i].SetBounds(buttonX, Px(LogoTop) + i * step, size, size);

        int pillHeight = Px(22);
        int pillY = Px(FooterTop);
        _doubleButton.SetBounds(Px(BaseWidth - Pad - 44), pillY, Px(44), pillHeight);
        _halveButton.SetBounds(Px(BaseWidth - Pad - 44 - 6 - 40), pillY, Px(40), pillHeight);

        RebuildFonts();
        Native.ApplyRoundedCorners(this, Px(CornerRadius));
        Invalidate();
    }

    private void RebuildFonts()
    {
        _numberFont?.Dispose();
        _decimalFont?.Dispose();
        _unitFont?.Dispose();
        _footerFont?.Dispose();
        _pillFont?.Dispose();

        _numberFont = EmbeddedFonts.Get(72f);
        _decimalFont = EmbeddedFonts.Get(28f);
        _unitFont = EmbeddedFonts.Get(10f, FontStyle.Bold);
        _footerFont = EmbeddedFonts.Get(8.5f);
        _pillFont = EmbeddedFonts.Get(9f, FontStyle.Bold);

        _halveButton.Font = _pillFont;
        _doubleButton.Font = _pillFont;
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ApplyLayout();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Native.ApplyRoundedCorners(this, Px(CornerRadius));
    }

    // -- painting ---------------------------------------------------------------------

    private void ApplyPalette(Color background)
    {
        _background = background;
        _ink = Theme.InkOn(background);
        _mutedInk = Theme.MutedInkOn(background);
        BackColor = background;
        _halveButton.Ink = _ink;
        _doubleButton.Ink = _ink;

        if (_logoSource is not null)
        {
            Image? previous = _logo.Image;
            _logo.Image = ImageTint.Recolour(_logoSource, _ink);
            previous?.Dispose();
        }

        Invalidate();
    }

    private Color NextBackground()
    {
        Color next;
        do { next = Theme.RandomBackground(); }
        while (next == _background && Theme.Backgrounds.Length > 1);
        return next;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // A tap briefly lifts the whole background, which reads better than flashing
        // a single element and works no matter which palette colour is in play.
        Color surface = _pulse > 0 ? Theme.Lighten(_background, 0.10 * _pulse) : _background;
        using var brush = new SolidBrush(surface);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        DrawTempo(g);
        DrawStabilityMeter(g);
        DrawFooter(g);
    }

    private void DrawTempo(Graphics g)
    {
        (string whole, string fraction) = FormatBpm();

        using var brush = new SolidBrush(_ink);
        using var mutedBrush = new SolidBrush(_mutedInk);
        StringFormat format = StringFormat.GenericTypographic;

        float x = Px(Pad);
        float baseline = Px(NumberTop + NumberHeight);

        // Scaling the whole group around its bottom-left corner nudges the reading upward
        // on each tap without letting it drift out of the layout.
        GraphicsState state = g.Save();
        if (_pulse > 0)
        {
            float scale = 1f + (float)(0.045 * _pulse);
            g.TranslateTransform(x, baseline);
            g.ScaleTransform(scale, scale);
            g.TranslateTransform(-x, -baseline);
        }

        SizeF wholeSize = g.MeasureString(whole, _numberFont, PointF.Empty, format);
        g.DrawString(whole, _numberFont, brush, x, baseline - wholeSize.Height, format);

        // The decimal is set small so three digits plus a fraction still fit beside the
        // buttons at the size the design wants for the number itself.
        float cursor = x + wholeSize.Width;
        if (fraction.Length > 0)
        {
            cursor -= Px(8);   // close the gap the monospace advance leaves before the dot
            SizeF fractionSize = g.MeasureString(fraction, _decimalFont, PointF.Empty, format);
            g.DrawString(fraction, _decimalFont, brush, cursor, baseline - fractionSize.Height, format);
            cursor += fractionSize.Width;
        }

        if (_tempo.Bpm is not null)
        {
            SizeF unitSize = g.MeasureString("BPM", _unitFont, PointF.Empty, format);
            g.DrawString("BPM", _unitFont, mutedBrush, cursor + Px(7), baseline - unitSize.Height, format);
        }

        g.Restore(state);
    }

    private void DrawStabilityMeter(Graphics g)
    {
        if (_tempo.Stability is not double stability)
            return;

        float x = Px(Pad);
        float y = Px(MeterTop);
        float width = Px(112);
        float height = Math.Max(2f, Px(3));

        using var track = new SolidBrush(Color.FromArgb(48, _ink));
        using var fill = new SolidBrush(Color.FromArgb(200, _ink));
        g.FillRectangle(track, x, y, width, height);
        g.FillRectangle(fill, x, y, (float)(width * stability), height);
    }

    private void DrawFooter(Graphics g)
    {
        using var brush = new SolidBrush(_mutedInk);
        var area = new RectangleF(Px(Pad), Px(FooterTop), Px(BaseWidth), Px(FooterHeight));
        using var format = new StringFormat(StringFormat.GenericTypographic)
        {
            LineAlignment = StringAlignment.Center,
        };
        g.DrawString(FooterText(), _footerFont, brush, area, format);
    }

    /// <summary>Splits the reading into the large integer part and the small fractional part.</summary>
    private (string Whole, string Fraction) FormatBpm()
    {
        if (_tempo.Bpm is not double bpm)
            return ("0", "");

        // One decimal is what a producer actually needs, but only once there are enough
        // taps for it to mean anything.
        if (_tempo.TapCount < 4)
            return (Math.Round(bpm).ToString("0"), "");

        string text = bpm.ToString("0.0");
        int dot = text.IndexOf('.');
        return (text[..dot], text[dot..]);
    }

    private string FooterText()
    {
        if (_tempo.TapCount == 0)
            return _hotkey.IsRegistered ? $"tap:  space  ·  {_hotkey.Describe()}" : "tap:  space";

        return $"{_tempo.TapCount} tap{(_tempo.TapCount == 1 ? "" : "s")}";
    }

    // -- behaviour --------------------------------------------------------------------

    private void Tap()
    {
        TapTempo.TapResult result = _tempo.Tap();
        if (result == TapTempo.TapResult.Ignored)
            return;

        UpdateTempoButtons();

        _pulse = 1;
        if (!_animation.Enabled)
            _animation.Start();

        if (_metronome.IsRunning && _tempo.Bpm is double bpm)
            _metronome.Bpm = bpm;

        Invalidate();
    }

    private void AdvanceAnimation()
    {
        // ~180 ms decay at a 16 ms tick.
        _pulse -= 0.09;
        if (_pulse <= 0)
        {
            _pulse = 0;
            _animation.Stop();
        }
        Invalidate();
    }

    private void UpdateTempoButtons()
    {
        bool hasTempo = _tempo.Bpm is not null;
        _halveButton.Visible = hasTempo;
        _doubleButton.Visible = hasTempo;
    }

    private void ResetTempo()
    {
        _tempo.Reset();
        _metronome.Stop();
        _metronomeButton.Active = false;
        UpdateTempoButtons();
        Invalidate();
    }

    private void ScaleTempo(double factor)
    {
        _tempo.Scale(factor);
        if (_metronome.IsRunning && _tempo.Bpm is double bpm)
            _metronome.Bpm = bpm;
        Invalidate();
    }

    private void ToggleMetronome()
    {
        if (_tempo.Bpm is not double bpm)
            return;   // nothing to play yet

        _metronome.Bpm = bpm;
        _metronome.Toggle();
        _metronomeButton.Active = _metronome.IsRunning;
    }

    private void SetAlwaysOnTop(bool enabled)
    {
        TopMost = enabled;
        _pinButton.Active = enabled;
        _settings.AlwaysOnTop = enabled;
    }

    private void SetGlobalHotkey(bool enabled)
    {
        _settings.GlobalHotkeyEnabled = enabled;

        if (!enabled)
        {
            _hotkey.Unregister();
        }
        else
        {
            _hotkeyUnavailable = !_hotkey.RegisterFirstAvailable(HotkeyCandidates);
        }

        Invalidate();
    }

    private void CopyBpm()
    {
        if (_tempo.Bpm is not double bpm)
            return;

        try
        {
            Clipboard.SetText(bpm.ToString("0.0"));
        }
        catch (ExternalException)
        {
            // Another process had the clipboard locked; nothing useful to do.
        }
    }

    private void OpenSpotify()
    {
        try
        {
            Process.Start(new ProcessStartInfo(SpotifyUrl) { UseShellExecute = true });
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            MessageBox.Show(this, "Could not open the link.", "Tap BPM",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // -- input ------------------------------------------------------------------------

    /// <summary>The strip above the number drags the window; the rest of it taps.</summary>
    private bool IsDragArea(Point p) => p.Y < Px(NumberTop);

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (IsDragArea(e.Location))
                Native.BeginWindowDrag(this);
            else
                Tap();
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        Cursor = IsDragArea(e.Location) ? Cursors.SizeAll : Cursors.Hand;
        base.OnMouseMove(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Handled here so the keys work regardless of which child control has focus, and
        // so a held-down key does not machine-gun taps the way the original could.
        switch (keyData)
        {
            case Keys.Space:
            case Keys.Enter:
                Tap();
                return true;
            case Keys.R:
            case Keys.Back:
                ResetTempo();
                return true;
            case Keys.T:
                SetAlwaysOnTop(!TopMost);
                return true;
            case Keys.M:
                ToggleMetronome();
                return true;
            case Keys.Control | Keys.C:
                CopyBpm();
                return true;
            case Keys.Escape:
                Close();
                return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // -- lifetime ---------------------------------------------------------------------

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _settings.RememberPosition(Location);
        _settings.Save();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animation.Dispose();
            _metronome.Dispose();
            _hotkey.Dispose();
            _tips.Dispose();
            _logoSource?.Dispose();
            _logo.Image?.Dispose();
            _numberFont?.Dispose();
            _decimalFont?.Dispose();
            _unitFont?.Dispose();
            _footerFont?.Dispose();
            _pillFont?.Dispose();
        }
        base.Dispose(disposing);
    }

    // -- resources --------------------------------------------------------------------

    private static Icon? LoadAppIcon() => LoadEmbedded("tap-bpm.ico", s => new Icon(s));

    // Image.FromStream keeps a reference to the stream for the life of the image, so the
    // pixels are copied into a standalone bitmap before the stream is closed.
    private static Image? LoadLogo() => LoadEmbedded("logo.png", stream =>
    {
        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    });

    private static T? LoadEmbedded<T>(string fileName, Func<Stream, T> factory) where T : class
    {
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string? name = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
            if (name is null)
                return null;

            using Stream stream = assembly.GetManifestResourceStream(name)!;
            return factory(stream);
        }
        catch (Exception e) when (e is IOException or ArgumentException)
        {
            return null;
        }
    }
}
