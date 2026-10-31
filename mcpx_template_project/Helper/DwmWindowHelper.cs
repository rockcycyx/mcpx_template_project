using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace mcpx_template_project.Helper
{
    /// <summary>
    /// DWM 窗口背景（ui:WindowHelper.SystemBackdropType=Acrylic/Mica）辅助。
    /// 两个经过实测（运行时 SetWindowLong 探针）确认的关键点：
    /// 1. DWM 背景只绘制在带 WS_THICKFRAME 样式的窗口上。WindowStyle=None + ResizeMode=NoResize
    ///    会移除该样式，导致背景完全不绘制（整个窗口呈现为一片纯色）。需运行时补回；
    ///    且注入必须发生在 Loaded 之后——WindowChrome 会在 SourceInitialized 前后接管并重算窗口样式，
    ///    过早注入的 WS_THICKFRAME 会被剥掉。补回后窗口视觉仍无边框，且
    ///    WindowChrome(ResizeBorderThickness=0) 拦截边缘命中测试，窗口实际仍不可缩放。
    /// 2. DWM 背景材质的明暗变体由 DWMWA_USE_IMMERSIVE_DARK_MODE 决定，缺省为浅色材质；
    ///    需按应用当前主题设置，否则深色主题下背景发白、看起来"不跟随系统"。
    /// </summary>
    public static class DwmWindowHelper
    {
        private const int GWL_STYLE = -16;
        private const int WS_THICKFRAME = 0x00040000;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong(nint hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int value, int size);

        /// <summary>在窗口 SourceInitialized 时调用：Loaded 后补回 WS_THICKFRAME，并让 DWM 背景材质跟随应用明暗主题。</summary>
        public static void ApplyWindowBackdrop(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == nint.Zero) return;

            // 延迟到 Loaded 之后（再让 Dispatcher 跑一拍）：等 WindowChrome 完成窗口样式接管后再注入
            window.Loaded += (_, _) => window.Dispatcher.BeginInvoke(new Action(() =>
            {
                int style = GetWindowLong(hwnd, GWL_STYLE);
                if ((style & WS_THICKFRAME) == 0)
                {
                    SetWindowLong(hwnd, GWL_STYLE, style | WS_THICKFRAME);
                    SetWindowPos(hwnd, nint.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_FRAMECHANGED);
                }

                int dark = IsDarkTheme() ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
            }), DispatcherPriority.Loaded);
        }

        /// <summary>通过应用级 Fluent 资源解析出的 SolidBackgroundFillColorBase 判断当前明暗主题。
        /// 注意必须用 TryFindResource（穿透合并字典），ResourceDictionary.Contains 不搜索 MergedDictionaries。</summary>
        private static bool IsDarkTheme()
        {
            var c = Application.Current?.TryFindResource("SolidBackgroundFillColorBase") as Color?;
            return c.HasValue && c.Value.R + c.Value.G + c.Value.B < 384; // Dark=#202020(96)，Light=#F3F3F3(729)
        }
    }
}
