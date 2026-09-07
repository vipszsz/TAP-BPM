using System.Drawing;
using System.Drawing.Imaging;

namespace TapBpm.Ui;

internal static class ImageTint
{
    /// <summary>
    /// Recolours an image while keeping its alpha, so one artwork works on every background.
    /// </summary>
    /// <remarks>
    /// The logo ships as black artwork, which disappears against the darker palette colours.
    /// Tinting it to the current ink colour keeps it legible on all five without shipping a
    /// second copy of the asset.
    /// </remarks>
    public static Bitmap Recolour(Image source, Color colour)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);

        // Zero out the incoming RGB and add the target colour back through the matrix's
        // translation row; the alpha row is left as an identity so edges stay smooth.
        var matrix = new ColorMatrix(new[]
        {
            new[] { 0f, 0f, 0f, 0f, 0f },
            new[] { 0f, 0f, 0f, 0f, 0f },
            new[] { 0f, 0f, 0f, 0f, 0f },
            new[] { 0f, 0f, 0f, 1f, 0f },
            new[] { colour.R / 255f, colour.G / 255f, colour.B / 255f, 0f, 1f },
        });

        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(matrix);

        using var g = Graphics.FromImage(result);
        g.Clear(Color.Transparent);
        g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
            0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);

        return result;
    }
}
