using System.Drawing;

namespace TapBpm.Ui;

/// <summary>The original app's palette, plus the derived tones the UI needs.</summary>
public static class Theme
{
    /// <summary>One of these is picked at random per launch, as in the first version.</summary>
    public static readonly Color[] Backgrounds =
    {
        ColorTranslator.FromHtml("#8f9a9c"),
        ColorTranslator.FromHtml("#e2765a"),
        ColorTranslator.FromHtml("#e3d55a"),
        ColorTranslator.FromHtml("#855be1"),
        ColorTranslator.FromHtml("#5ebe74"),
    };

    public static readonly Color ButtonFace = ColorTranslator.FromHtml("#1c1c1c");
    public static readonly Color ButtonFaceHover = ColorTranslator.FromHtml("#2e2e2e");
    public static readonly Color ButtonGlyph = ColorTranslator.FromHtml("#e8e8e8");
    public static readonly Color Danger = ColorTranslator.FromHtml("#d64545");

    private static readonly Color DarkInk = ColorTranslator.FromHtml("#1c1c1c");

    public static Color RandomBackground() =>
        Backgrounds[Random.Shared.Next(Backgrounds.Length)];

    /// <summary>
    /// White reads well on most of the palette but not on the yellow, so the foreground
    /// follows the background's luminance instead of being hard-coded.
    /// </summary>
    public static Color InkOn(Color background) =>
        Luminance(background) > 0.6 ? DarkInk : Color.White;

    /// <summary>A muted version of the ink, for secondary text.</summary>
    public static Color MutedInkOn(Color background) =>
        Blend(InkOn(background), background, 0.38);

    public static Color Lighten(Color c, double amount) =>
        Blend(c, Color.White, amount);

    public static Color Blend(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromArgb(
            (int)Math.Round(a.R + (b.R - a.R) * t),
            (int)Math.Round(a.G + (b.G - a.G) * t),
            (int)Math.Round(a.B + (b.B - a.B) * t));
    }

    /// <summary>Relative luminance, sRGB gamma corrected (WCAG definition).</summary>
    private static double Luminance(Color c)
    {
        static double Channel(int v)
        {
            double s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }
}
