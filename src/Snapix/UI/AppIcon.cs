using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Snapix.UI
{
    /// <summary>
    /// 运行时生成的应用图标。极简风格：方形选框中带十字。
    /// </summary>
    internal static class AppIcon
    {
        public static Icon Create(int size = 32, bool darkBackground = true)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                Color stroke = darkBackground ? Color.White : Color.FromArgb(30, 30, 32);

                // 边框选框（圆角矩形虚线四角）
                float pad = size * 0.18f;
                var rect = new RectangleF(pad, pad, size - pad * 2, size - pad * 2);

                using (var pen = new Pen(stroke, System.Math.Max(1.5f, size * 0.08f)))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;

                    // 四角 L 形
                    float corner = (size - pad * 2) * 0.32f;
                    // 左上
                    g.DrawLine(pen, rect.Left, rect.Top + corner, rect.Left, rect.Top);
                    g.DrawLine(pen, rect.Left, rect.Top, rect.Left + corner, rect.Top);
                    // 右上
                    g.DrawLine(pen, rect.Right - corner, rect.Top, rect.Right, rect.Top);
                    g.DrawLine(pen, rect.Right, rect.Top, rect.Right, rect.Top + corner);
                    // 右下
                    g.DrawLine(pen, rect.Right, rect.Bottom - corner, rect.Right, rect.Bottom);
                    g.DrawLine(pen, rect.Right, rect.Bottom, rect.Right - corner, rect.Bottom);
                    // 左下
                    g.DrawLine(pen, rect.Left + corner, rect.Bottom, rect.Left, rect.Bottom);
                    g.DrawLine(pen, rect.Left, rect.Bottom, rect.Left, rect.Bottom - corner);
                }

                // 中央实心圆点（强调点 + 标识）
                float dotR = size * 0.11f;
                float cx = size / 2f, cy = size / 2f;
                using (var brush = new SolidBrush(Theme.Accent))
                    g.FillEllipse(brush, cx - dotR, cy - dotR, dotR * 2, dotR * 2);
            }

            return BitmapToIcon(bmp);
        }

        private static Icon BitmapToIcon(Bitmap bmp)
        {
            byte[] pngBytes;
            using (var pngStream = new MemoryStream())
            {
                bmp.Save(pngStream, ImageFormat.Png);
                pngBytes = pngStream.ToArray();
            }

            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                // ICONDIR
                bw.Write((short)0);          // reserved
                bw.Write((short)1);          // type = icon
                bw.Write((short)1);          // count

                // ICONDIRENTRY
                bw.Write((byte)bmp.Width);
                bw.Write((byte)bmp.Height);
                bw.Write((byte)0);           // colors
                bw.Write((byte)0);           // reserved
                bw.Write((short)1);          // planes
                bw.Write((short)32);         // bpp
                bw.Write(pngBytes.Length);   // size
                bw.Write(22);                // offset (header size)

                // PNG payload
                bw.Write(pngBytes);
            }

            ms.Position = 0;
            // Icon ctor copies the stream content, ok to dispose ms after.
            var icon = new Icon(ms);
            ms.Dispose();
            return icon;
        }
    }
}
