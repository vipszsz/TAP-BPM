<#
.SYNOPSIS
    Builds a multi-resolution .ico from a source image.
.DESCRIPTION
    Windows needs 16/24/32/48 px frames for Explorer, the taskbar and Alt+Tab.
    A single 256x256 PNG frame (what the original icon shipped with) renders
    blank or badly scaled in those places, so every size is written explicitly:
    BMP/DIB frames up to 48 px for maximum shell compatibility, PNG above that
    to keep the file small.
#>
param(
    [string]$Source = "$PSScriptRoot\..\src\TapBpm\Assets\icon-256-source.ico",
    [string]$Output = "$PSScriptRoot\..\src\TapBpm\Assets\tap-bpm.ico"
)

Add-Type -AssemblyName System.Drawing

Add-Type @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class IcoWriter
{
    static Bitmap Resize(Image src, int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CompositingMode = CompositingMode.SourceCopy;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            using (var attr = new ImageAttributes())
            {
                attr.SetWrapMode(WrapMode.TileFlipXY); // kills the transparent halo at the edges
                g.DrawImage(src, new Rectangle(0, 0, size, size), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attr);
            }
        }
        return bmp;
    }

    // 32bpp DIB with the trailing AND mask that the ICO format still expects.
    static byte[] EncodeDib(Bitmap bmp)
    {
        int w = bmp.Width, h = bmp.Height;
        int maskStride = ((w + 31) / 32) * 4;
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(40); bw.Write(w); bw.Write(h * 2);
            bw.Write((short)1); bw.Write((short)32);
            bw.Write(0); bw.Write(w * h * 4 + maskStride * h);
            bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);

            for (int y = h - 1; y >= 0; y--)          // DIB rows run bottom-up
                for (int x = 0; x < w; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    bw.Write(c.B); bw.Write(c.G); bw.Write(c.R); bw.Write(c.A);
                }

            for (int y = h - 1; y >= 0; y--)
            {
                var row = new byte[maskStride];
                for (int x = 0; x < w; x++)
                    if (bmp.GetPixel(x, y).A == 0)
                        row[x / 8] |= (byte)(0x80 >> (x % 8));
                bw.Write(row);
            }
            return ms.ToArray();
        }
    }

    static byte[] EncodePng(Bitmap bmp)
    {
        using (var ms = new MemoryStream())
        {
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }

    public static void Build(string sourcePath, string outputPath, int[] sizes)
    {
        using (var src = Image.FromFile(sourcePath))
        {
            var frames = new List<KeyValuePair<int, byte[]>>();
            foreach (int size in sizes)
                using (var bmp = Resize(src, size))
                    frames.Add(new KeyValuePair<int, byte[]>(size, size <= 48 ? EncodeDib(bmp) : EncodePng(bmp)));

            using (var fs = File.Create(outputPath))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((short)0); bw.Write((short)1); bw.Write((short)frames.Count);
                int offset = 6 + 16 * frames.Count;
                foreach (var f in frames)
                {
                    bw.Write((byte)(f.Key >= 256 ? 0 : f.Key));   // 0 means 256 in the ICO header
                    bw.Write((byte)(f.Key >= 256 ? 0 : f.Key));
                    bw.Write((byte)0); bw.Write((byte)0);
                    bw.Write((short)1); bw.Write((short)32);
                    bw.Write(f.Value.Length); bw.Write(offset);
                    offset += f.Value.Length;
                }
                foreach (var f in frames) bw.Write(f.Value);
            }
        }
    }
}
'@ -ReferencedAssemblies System.Drawing

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
[IcoWriter]::Build((Resolve-Path $Source).Path, $Output, $sizes)
Write-Host ("Wrote {0} ({1} frames, {2:N0} bytes)" -f $Output, $sizes.Count, (Get-Item $Output).Length)
