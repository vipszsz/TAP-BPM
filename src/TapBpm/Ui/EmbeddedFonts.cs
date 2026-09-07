using System.Drawing;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TapBpm.Ui;

/// <summary>
/// Loads IBM Plex Mono out of the executable.
/// </summary>
/// <remarks>
/// The first version asked Windows for "IBM Plex Mono" by name, which meant the layout
/// silently fell apart on any machine where the font was not installed. Embedding it makes
/// the app look the same everywhere, and IBM Plex is OFL licensed so it may be shipped.
/// </remarks>
public static class EmbeddedFonts
{
    private static readonly PrivateFontCollection Collection = new();
    private static readonly List<IntPtr> Buffers = new();   // must outlive the collection
    private static FontFamily? _regular;
    private static FontFamily? _bold;

    public static FontFamily Regular => _regular ??= Load("IBMPlexMono-Regular.ttf", FontStyle.Regular);
    public static FontFamily Bold => _bold ??= Load("IBMPlexMono-Bold.ttf", FontStyle.Bold);

    public static Font Get(float sizePoints, FontStyle style = FontStyle.Regular)
    {
        FontFamily family = style.HasFlag(FontStyle.Bold) ? Bold : Regular;
        // The bold face is a separate family here, so ask it for its own regular style.
        FontStyle available = family.IsStyleAvailable(style) ? style : FontStyle.Regular;
        return new Font(family, sizePoints, available, GraphicsUnit.Point);
    }

    private static FontFamily Load(string fileName, FontStyle fallbackStyle)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resource = assembly.GetManifestResourceNames()
                .First(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

            using Stream stream = assembly.GetManifestResourceStream(resource)!;
            byte[] data = new byte[stream.Length];
            stream.ReadExactly(data);

            IntPtr buffer = Marshal.AllocCoTaskMem(data.Length);
            Marshal.Copy(data, 0, buffer, data.Length);
            Buffers.Add(buffer);

            int before = Collection.Families.Length;
            Collection.AddMemoryFont(buffer, data.Length);

            // AddMemoryFont appends, so the family just added is the last one.
            return Collection.Families.Length > before
                ? Collection.Families[^1]
                : Collection.Families[0];
        }
        catch
        {
            // Losing the exact typeface is not worth crashing over.
            return new FontFamily(GenericFontFamilies.Monospace);
        }
    }
}
