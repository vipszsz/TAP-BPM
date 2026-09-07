using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace TapBpm.Ui;

/// <summary>Win32 bits the borderless window needs.</summary>
internal static class Native
{
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>Hands the drag over to Windows, so it behaves like a real title bar.</summary>
    public static void BeginWindowDrag(IWin32Window window)
    {
        ReleaseCapture();
        SendMessage(window.Handle, WM_NCLBUTTONDOWN, HTCAPTION, IntPtr.Zero);
    }

    /// <summary>
    /// Rounds the window corners to match the app's design.
    /// </summary>
    /// <remarks>
    /// Windows 11 rounds them itself, anti-aliased and with a correct shadow. Older builds
    /// reject the attribute, so those fall back to a clipping region, which is blockier but
    /// still keeps the intended shape.
    /// </remarks>
    public static void ApplyRoundedCorners(Form form, int fallbackRadius)
    {
        int preference = DWMWCP_ROUND;
        try
        {
            if (DwmSetWindowAttribute(form.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int)) == 0)
                return;
        }
        catch (DllNotFoundException)
        {
            // dwmapi is always present on supported Windows versions; fall through anyway.
        }

        ApplyRegionFallback(form, fallbackRadius);
    }

    private static void ApplyRegionFallback(Form form, int radius)
    {
        int w = form.Width;
        int h = form.Height;
        int d = radius * 2;

        using var path = new GraphicsPath();
        path.AddArc(0, 0, d, d, 180, 90);
        path.AddArc(w - d, 0, d, d, 270, 90);
        path.AddArc(w - d, h - d, d, d, 0, 90);
        path.AddArc(0, h - d, d, d, 90, 90);
        path.CloseFigure();

        form.Region?.Dispose();
        form.Region = new Region(path);
    }
}
