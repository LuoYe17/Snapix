using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Snapix.Native
{
    /// <summary>
    /// 枚举可见窗口，用于智能窗口吸附。
    /// </summary>
    internal static class WindowFinder
    {
        public static List<Rectangle> GetVisibleWindowRects()
        {
            var rects = new List<Rectangle>();

            NativeMethods.EnumWindows((hWnd, _) =>
            {
                if (!NativeMethods.IsWindowVisible(hWnd))
                    return true;

                if (NativeMethods.GetWindowTextLength(hWnd) == 0)
                    return true;

                // 优先使用 DWM 获取真实边界（排除阴影）
                if (TryGetDwmRect(hWnd, out var rect))
                {
                    if (rect.Width > 0 && rect.Height > 0)
                        rects.Add(rect);
                }
                else if (NativeMethods.GetWindowRect(hWnd, out var wr))
                {
                    var r = new Rectangle(wr.Left, wr.Top, wr.Right - wr.Left, wr.Bottom - wr.Top);
                    if (r.Width > 0 && r.Height > 0)
                        rects.Add(r);
                }

                return true;
            }, IntPtr.Zero);

            return rects;
        }

        private static bool TryGetDwmRect(IntPtr hWnd, out Rectangle rect)
        {
            rect = Rectangle.Empty;
            int size = Marshal.SizeOf<NativeMethods.RECT>();
            int hr = NativeMethods.DwmGetWindowAttribute(hWnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
                out var dwmRect, size);

            if (hr == 0)
            {
                rect = new Rectangle(dwmRect.Left, dwmRect.Top,
                    dwmRect.Right - dwmRect.Left, dwmRect.Bottom - dwmRect.Top);
                return true;
            }

            return false;
        }
    }
}
