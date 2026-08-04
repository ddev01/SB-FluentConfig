# Generates Assets/FluentConfig.ico — ON toggle mark in accent green (#00C47A).
Add-Type -AssemblyName System.Drawing

$src = @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public static class FluentConfigIconBuilder
{
    public static void Save(string path, int[] sizes)
    {
        using (var fs = File.Create(path))
        using (var bw = new BinaryWriter(fs))
        {
            var frames = new byte[sizes.Length][];
            for (int i = 0; i < sizes.Length; i++)
                frames[i] = EncodeBmpFrame(DrawToggle(sizes[i]));

            bw.Write((ushort)0);
            bw.Write((ushort)1);
            bw.Write((ushort)frames.Length);

            int offset = 6 + (16 * frames.Length);
            for (int i = 0; i < frames.Length; i++)
            {
                int s = sizes[i];
                bw.Write((byte)(s >= 256 ? 0 : s));
                bw.Write((byte)(s >= 256 ? 0 : s));
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((ushort)1);
                bw.Write((ushort)32);
                bw.Write(frames[i].Length);
                bw.Write(offset);
                offset += frames[i].Length;
            }

            for (int i = 0; i < frames.Length; i++)
                bw.Write(frames[i]);
        }
    }

    static Bitmap DrawToggle(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        bmp.SetResolution(96, 96);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.FromArgb(0, 0, 0, 0));

            // Match ToggleControl ON: bg-fc-accent (#00C47A) + white knob on the right.
            var accent = Color.FromArgb(255, 0, 196, 122);
            int pad = Math.Max(1, (int)Math.Round(size * 0.10));
            int trackH = Math.Max(8, (int)Math.Round(size * 0.44));
            int trackW = size - (2 * pad);
            float trackX = pad;
            float trackY = (size - trackH) / 2f;

            using (var path = Capsule(trackX, trackY, trackW, trackH))
            using (var brush = new SolidBrush(accent))
                g.FillPath(brush, path);

            int knobPad = Math.Max(1, (int)Math.Round(trackH * 0.12));
            int knobD = trackH - (2 * knobPad);
            float knobX = trackX + trackW - knobPad - knobD;
            float knobY = trackY + knobPad;
            using (var brush = new SolidBrush(Color.White))
                g.FillEllipse(brush, knobX, knobY, knobD, knobD);
        }
        return bmp;
    }

    static GraphicsPath Capsule(float x, float y, float w, float h)
    {
        var path = new GraphicsPath();
        path.AddArc(x, y, h, h, 90, 180);
        path.AddArc(x + w - h, y, h, h, 270, 180);
        path.CloseFigure();
        return path;
    }

    static byte[] EncodeBmpFrame(Bitmap bmp)
    {
        int w = bmp.Width, h = bmp.Height;
        int xorStride = w * 4;
        int andRowBytes = ((w + 31) / 32) * 4;
        int xorSize = xorStride * h;
        int andSize = andRowBytes * h;

        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(40);
            bw.Write(w);
            bw.Write(h * 2);
            bw.Write((ushort)1);
            bw.Write((ushort)32);
            bw.Write(0);
            bw.Write(xorSize + andSize);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);

            var data = bmp.LockBits(
                new Rectangle(0, 0, w, h),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                var row = new byte[xorStride];
                for (int y = h - 1; y >= 0; y--)
                {
                    Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, xorStride);
                    bw.Write(row);
                }
            }
            finally
            {
                bmp.UnlockBits(data);
                bmp.Dispose();
            }

            bw.Write(new byte[andSize]);
            bw.Flush();
            return ms.ToArray();
        }
    }
}
'@

try {
    Add-Type -TypeDefinition $src -ReferencedAssemblies System.Drawing
} catch {
    # Type already loaded in this session — rebuild by using a unique name is unnecessary for one-shot runs.
    if ($_.Exception.Message -notmatch 'already exists') { throw }
}

$dir = Join-Path $PSScriptRoot '..\Assets'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$out = Join-Path $dir 'FluentConfig.ico'
[FluentConfigIconBuilder]::Save($out, [int[]]@(16, 24, 32, 48))
Write-Host "Created $out"
