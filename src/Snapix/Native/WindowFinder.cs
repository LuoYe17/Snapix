using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Snapix.Native
{
    /// <summary>
    /// 枚举可见窗口，用于智能窗口吸附。
    /// </summary>
    internal static class WindowFinder
    {
        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        // 桌面/任务栏类名，必须排除，否则吸附会变成全屏
        private static readonly HashSet<string> ExcludedClasses = new HashSet<string>
        {
            "Progman",          // 桌面
            "WorkerW",          // 桌面工作区
            "Shell_TrayWnd",    // 任务栏
            "Shell_SecondaryTrayWnd", // 多屏副任务栏
            "TrayNotifyWnd",
            "ReBarWindow32",
            "Button",           // "开始"按钮
        };

        /// <summary>
        /// 枚举可吸附的可见窗口边界。
        /// </summary>
        /// <param name="excludeHandle">要排除的窗口（通常是截图遮罩本身）。</param>
        /// <param name="virtualBounds">虚拟桌面边界，用于过滤覆盖整个虚拟桌面的窗口。</param>
        public static List<Rectangle> GetVisibleWindowRects(IntPtr excludeHandle, Rectangle virtualBounds)
        {
            var rects = new List<Rectangle>();

            NativeMethods.EnumWindows((hWnd, _) =>
            {
                if (hWnd == excludeHandle) return true;
                if (!NativeMethods.IsWindowVisible(hWnd)) return true;
                if (IsIconic(hWnd)) return true;
                if (NativeMethods.GetWindowTextLength(hWnd) == 0) return true;

                // 排除桌面、任务栏等系统窗口
                var sb = new StringBuilder(64);
                GetClassName(hWnd, sb, sb.Capacity);
                if (ExcludedClasses.Contains(sb.ToString())) return true;

                Rectangle rect;
                if (TryGetDwmRect(hWnd, out rect))
                {
                    // ok
                }
                else if (NativeMethods.GetWindowRect(hWnd, out var wr))
                {
                    rect = new Rectangle(wr.Left, wr.Top, wr.Right - wr.Left, wr.Bottom - wr.Top);
                }
                else
                {
                    return true;
                }

                if (rect.Width <= 0 || rect.Height <= 0) return true;

                // 排除尺寸 >= 虚拟桌面的窗口（避免"全屏吸附"陷阱）
                if (rect.Width >= virtualBounds.Width && rect.Height >= virtualBounds.Height)
                    return true;

                rects.Add(rect);
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
