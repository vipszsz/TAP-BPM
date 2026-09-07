using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace TapBpm.Ui;

public enum Glyph { Close, Reset, Pin, Metronome }

/// <summary>
/// The round dark buttons from the original UI, drawn with GDI+ instead of loaded from PNGs.
/// </summary>
/// <remarks>
/// Vector drawing keeps them sharp at any DPI and lets new buttons be added without
/// authoring new bitmaps. Toggle buttons invert their colours to show they are on.
/// </remarks>
public sealed class IconButton : Control
{
    private bool _hovered;
    private bool _pressed;
    private bool _active;

    public IconButton(Glyph glyph)
    {
        Glyph = glyph;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    public Glyph Glyph { get; }

    /// <summary>Tints the button red on hover. Used for close.</summary>
    [DefaultValue(false)]
    public bool DangerOnHover { get; init; }

    /// <summary>Whether this button is a toggle that is currently on.</summary>
    [DefaultValue(false)]
    public bool Active
    {
        get => _active;
        set { if (_active != value) { _active = value; Invalidate(); } }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var box = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        if (_pressed)
            box.Inflate(-Width * 0.04f, -Height * 0.04f);

        Color face = _active ? Theme.ButtonGlyph
                   : DangerOnHover && _hovered ? Theme.Danger
                   : _hovered ? Theme.ButtonFaceHover
                   : Theme.ButtonFace;
        Color ink = _active ? Theme.ButtonFace : Theme.ButtonGlyph;

        using (var brush = new SolidBrush(face))
            g.FillEllipse(brush, box);

        DrawGlyph(g, box, ink);
    }

    private void DrawGlyph(Graphics g, RectangleF box, Color ink)
    {
        float size = Math.Min(box.Width, box.Height);
        float cx = box.X + box.Width / 2f;
        float cy = box.Y + box.Height / 2f;
        float stroke = Math.Max(1.2f, size * 0.09f);

        using var pen = new Pen(ink, stroke)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };
        using var brush = new SolidBrush(ink);

        switch (Glyph)
        {
            case Glyph.Close:
                DrawClose(g, pen, cx, cy, size);
                break;
            case Glyph.Reset:
                DrawReset(g, pen, brush, cx, cy, size);
                break;
            case Glyph.Pin:
                DrawPin(g, pen, brush, cx, cy, size);
                break;
            case Glyph.Metronome:
                DrawNote(g, pen, brush, cx, cy, size);
                break;
        }
    }

    private static void DrawClose(Graphics g, Pen pen, float cx, float cy, float size)
    {
        float r = size * 0.19f;
        g.DrawLine(pen, cx - r, cy - r, cx + r, cy + r);
        g.DrawLine(pen, cx + r, cy - r, cx - r, cy + r);
    }

    private static void DrawReset(Graphics g, Pen pen, Brush brush, float cx, float cy, float size)
    {
        // Counter-clockwise arc with an arrowhead, matching the original undo icon.
        float r = size * 0.22f;
        g.DrawArc(pen, new RectangleF(cx - r, cy - r, r * 2, r * 2), 20, 300);

        float tipX = cx - r;
        float tipY = cy - r * 0.18f;
        float head = size * 0.15f;
        using var arrow = new GraphicsPath();
        arrow.AddPolygon(new[]
        {
            new PointF(tipX, tipY - head * 0.75f),
            new PointF(tipX - head * 0.62f, tipY + head * 0.35f),
            new PointF(tipX + head * 0.62f, tipY + head * 0.35f),
        });
        g.FillPath(brush, arrow);
    }

    private static void DrawPin(Graphics g, Pen pen, Brush brush, float cx, float cy, float size)
    {
        // A push pin seen from the side: head, shoulders, needle.
        float h = size * 0.30f;
        using var path = new GraphicsPath();
        path.AddPolygon(new[]
        {
            new PointF(cx - h * 0.85f, cy - h * 0.75f),
            new PointF(cx + h * 0.85f, cy - h * 0.75f),
            new PointF(cx + h * 0.42f, cy - h * 0.30f),
            new PointF(cx + h * 0.62f, cy + h * 0.35f),
            new PointF(cx - h * 0.62f, cy + h * 0.35f),
            new PointF(cx - h * 0.42f, cy - h * 0.30f),
        });
        g.FillPath(brush, path);
        g.DrawLine(pen, cx, cy + h * 0.35f, cx, cy + h * 1.05f);
    }

    private static void DrawNote(Graphics g, Pen pen, Brush brush, float cx, float cy, float size)
    {
        // Eighth note: tilted head, stem, flag.
        float r = size * 0.13f;
        float headX = cx - size * 0.10f;
        float headY = cy + size * 0.16f;

        GraphicsState state = g.Save();
        g.TranslateTransform(headX, headY);
        g.RotateTransform(-20);
        g.FillEllipse(brush, -r * 1.25f, -r * 0.92f, r * 2.5f, r * 1.84f);
        g.Restore(state);

        float stemX = headX + r * 1.15f;
        float stemTop = cy - size * 0.22f;
        g.DrawLine(pen, stemX, headY - r * 0.55f, stemX, stemTop);

        using var flag = new GraphicsPath();
        flag.AddBezier(
            stemX, stemTop,
            stemX + size * 0.16f, stemTop + size * 0.06f,
            stemX + size * 0.15f, stemTop + size * 0.14f,
            stemX + size * 0.06f, stemTop + size * 0.21f);
        g.DrawPath(pen, flag);
    }
}
