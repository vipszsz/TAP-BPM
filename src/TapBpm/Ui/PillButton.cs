using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace TapBpm.Ui;

/// <summary>Small rounded text button, used for the halve and double tempo controls.</summary>
public sealed class PillButton : Control
{
    private bool _hovered;
    private bool _pressed;

    public PillButton(string caption)
    {
        Text = caption;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    /// <summary>Ink colour, kept in step with the window background by the form.</summary>
    [DefaultValue(typeof(Color), "White")]
    public Color Ink { get; set; } = Color.White;

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
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var box = new Rectangle(0, 0, Width - 1, Height - 1);
        float radius = box.Height / 2f;

        using var path = new GraphicsPath();
        path.AddArc(box.X, box.Y, radius * 2, box.Height, 90, 180);
        path.AddArc(box.Right - radius * 2, box.Y, radius * 2, box.Height, 270, 180);
        path.CloseFigure();

        int fillAlpha = _pressed ? 86 : _hovered ? 56 : 0;
        if (fillAlpha > 0)
        {
            using var brush = new SolidBrush(Color.FromArgb(fillAlpha, Ink));
            g.FillPath(brush, path);
        }

        using (var pen = new Pen(Color.FromArgb(_hovered ? 190 : 130, Ink), 1.4f))
            g.DrawPath(pen, path);

        TextRenderer.DrawText(g, Text, Font, box, Ink,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
