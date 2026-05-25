using System;
using System.Drawing;
using System.Windows.Forms;

namespace Snapix.Native
{
    /// <summary>
    /// 捕获所有屏幕的完整截图（虚拟桌面）。
    /// </summary>
    internal static class ScreenCapture
    {
        public static Bitmap CaptureAllScreens()
        {
            var bounds = GetVirtualScreenBounds();
            var bmp = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }

            return bmp;
        }

        public static Rectangle GetVirtualScreenBounds()
        {
            int left = SystemInformation.VirtualScreen.Left;
            int top = SystemInformation.VirtualScreen.Top;
            int width = SystemInformation.VirtualScreen.Width;
            int height = SystemInformation.VirtualScreen.Height;
            return new Rectangle(left, top, width, height);
        }
    }
}
