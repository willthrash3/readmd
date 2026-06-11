param(
    [string] $OutIcon = "src\Readmd\Assets\Readmd.ico",
    [string] $PreviewPng = "TestResults\readmd-icon-256.png"
)

$generatorSource = @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class ReadmdIconGenerator
{
    private sealed class IconFrame
    {
        public IconFrame(int size, byte[] png)
        {
            Size = size;
            Png = png;
        }

        public int Size { get; private set; }
        public byte[] Png { get; private set; }
    }

    public static void Generate(string iconPath, string previewPath)
    {
        var sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
        var frames = new List<IconFrame>();

        foreach (var size in sizes)
        {
            frames.Add(new IconFrame(size, RenderPng(size)));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(iconPath)));
        WriteIco(iconPath, frames);

        if (!string.IsNullOrWhiteSpace(previewPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(previewPath)));
            File.WriteAllBytes(previewPath, frames[frames.Count - 1].Png);
        }
    }

    private static byte[] RenderPng(int size)
    {
        using (var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.ScaleTransform(size / 256f, size / 256f);

            DrawIcon(graphics);

            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }

    private static void DrawIcon(Graphics graphics)
    {
        using (var background = RoundedRectangle(12, 12, 232, 232, 48))
        using (var brush = new LinearGradientBrush(new PointF(36, 28), new PointF(218, 228), Color.FromArgb(28, 126, 214), Color.FromArgb(11, 61, 112)))
        {
            graphics.FillPath(brush, background);
        }

        using (var shadow = DocumentPath(74, 48))
        using (var shadowBrush = new SolidBrush(Color.FromArgb(48, 4, 34, 64)))
        {
            graphics.FillPath(shadowBrush, shadow);
        }

        using (var page = DocumentPath(70, 42))
        using (var pageBrush = new SolidBrush(Color.White))
        using (var pagePen = new Pen(Color.FromArgb(214, 225, 236), 4))
        {
            graphics.FillPath(pageBrush, page);
            graphics.DrawPath(pagePen, page);
        }

        using (var fold = new GraphicsPath())
        using (var foldBrush = new SolidBrush(Color.FromArgb(221, 232, 245)))
        using (var foldPen = new Pen(Color.FromArgb(175, 197, 218), 4))
        {
            fold.AddPolygon(new[] { new PointF(160, 42), new PointF(194, 76), new PointF(160, 76) });
            graphics.FillPath(foldBrush, fold);
            graphics.DrawLines(foldPen, new[] { new PointF(160, 42), new PointF(160, 76), new PointF(194, 76) });
        }

        using (var markPen = new Pen(Color.FromArgb(16, 42, 67), 13))
        {
            markPen.StartCap = LineCap.Round;
            markPen.EndCap = LineCap.Round;
            markPen.LineJoin = LineJoin.Round;
            graphics.DrawLines(markPen, new[] { new PointF(90, 158), new PointF(90, 110), new PointF(106, 134), new PointF(122, 110), new PointF(122, 158) });
            graphics.DrawLine(markPen, 150, 108, 150, 154);
        }

        using (var arrow = new GraphicsPath())
        using (var markBrush = new SolidBrush(Color.FromArgb(16, 42, 67)))
        {
            arrow.AddPolygon(new[] { new PointF(132, 143), new PointF(150, 162), new PointF(168, 143) });
            graphics.FillPath(markBrush, arrow);
        }

        using (var primaryLine = new Pen(Color.FromArgb(107, 164, 217), 9))
        using (var secondaryLine = new Pen(Color.FromArgb(183, 206, 228), 8))
        {
            primaryLine.StartCap = LineCap.Round;
            primaryLine.EndCap = LineCap.Round;
            secondaryLine.StartCap = LineCap.Round;
            secondaryLine.EndCap = LineCap.Round;
            graphics.DrawLine(primaryLine, 92, 182, 172, 182);
            graphics.DrawLine(secondaryLine, 92, 198, 152, 198);
        }
    }

    private static GraphicsPath RoundedRectangle(float x, float y, float width, float height, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static GraphicsPath DocumentPath(float x, float y)
    {
        var path = new GraphicsPath();
        path.StartFigure();
        path.AddLine(x, y, 160, y);
        path.AddLine(160, y, 194, 76);
        path.AddLine(194, 76, 194, 210);
        path.AddLine(194, 210, x, 210);
        path.CloseFigure();
        return path;
    }

    private static void WriteIco(string iconPath, List<IconFrame> frames)
    {
        using (var file = new FileStream(iconPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new BinaryWriter(file))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)frames.Count);

            var offset = 6 + (16 * frames.Count);
            foreach (var frame in frames)
            {
                writer.Write((byte)(frame.Size >= 256 ? 0 : frame.Size));
                writer.Write((byte)(frame.Size >= 256 ? 0 : frame.Size));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(frame.Png.Length);
                writer.Write(offset);
                offset += frame.Png.Length;
            }

            foreach (var frame in frames)
            {
                writer.Write(frame.Png);
            }
        }
    }
}
"@

Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition $generatorSource -ReferencedAssemblies System.Drawing
[ReadmdIconGenerator]::Generate($OutIcon, $PreviewPng)

Write-Host "Generated $OutIcon"
if ($PreviewPng) {
    Write-Host "Generated $PreviewPng"
}
