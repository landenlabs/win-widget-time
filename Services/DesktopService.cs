// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WinWidgetTime.Services;

public static class DesktopService
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")] static extern IntPtr FindWindow(string cls, string? wnd);
    [DllImport("user32.dll")] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string? wnd);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr child, IntPtr newParent);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT pt);
    [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr hWnd, ref POINT pt);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);

    private const int GWL_EXSTYLE = -20;
    private const long WS_EX_TOOLWINDOW = 0x00000080;
    private const long WS_EX_APPWINDOW  = 0x00040000;

    private static readonly IntPtr HWND_BOTTOM = new(1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;

    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }

    /// <summary>
    /// Embeds the window into the desktop wallpaper WorkerW layer.
    /// Returns true on success, false if WorkerW was not found (fall back to SetAlwaysOnBottom).
    /// </summary>
    public static bool EmbedInWallpaper(Window window)
    {
        // Windows 10: reparenting a WPF AllowsTransparency=True window into WorkerW
        // leaves the HWND alive but DWM never composites it, so the widget renders
        // invisibly. Skip embedding on Win10 — caller falls back to SetAlwaysOnBottom.
        if (!IsWindows11OrLater()) return false;

        var handle = new WindowInteropHelper(window).Handle;
        var progman = FindWindow("Progman", null);

        // Tell Explorer to spawn a WorkerW behind desktop icons
        SendMessage(progman, 0x052C, IntPtr.Zero, IntPtr.Zero);

        IntPtr workerW = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            var defView = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero)
                workerW = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
            return true;
        }, IntPtr.Zero);

        if (workerW == IntPtr.Zero) return false;

        SetParent(handle, workerW);
        return true;
    }

    public static void SetAlwaysOnBottom(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        SetWindowPos(handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    /// <summary>
    /// Moves an embedded (child of WorkerW) window so its top-left lands at the given SCREEN pixel
    /// position. MoveWindow on a child takes parent-client coordinates, and WorkerW's origin is the
    /// virtual-screen origin — which is negative when a monitor sits left of / above the primary —
    /// so convert screen → parent-client first. Size defaults to the current window size.
    /// </summary>
    public static void MoveEmbeddedWindow(Window window, int screenX, int screenY, int? widthPx = null, int? heightPx = null)
    {
        var handle = new WindowInteropHelper(window).Handle;
        GetWindowRect(handle, out var rect);
        int w = widthPx  ?? rect.Right - rect.Left;
        int h = heightPx ?? rect.Bottom - rect.Top;

        var pt = new POINT { X = screenX, Y = screenY };
        var parent = GetParent(handle);
        if (parent != IntPtr.Zero)
            ScreenToClient(parent, ref pt);

        MoveWindow(handle, pt.X, pt.Y, w, h, true);
    }

    /// <summary>
    /// Adds WS_EX_TOOLWINDOW so the widget doesn't appear in Alt-Tab / Task View
    /// (ShowInTaskbar="False" alone only hides the taskbar button).
    /// </summary>
    public static void HideFromAltTab(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        long ex = GetWindowLongPtr(handle, GWL_EXSTYLE).ToInt64();
        SetWindowLongPtr(handle, GWL_EXSTYLE, new IntPtr((ex | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW));
    }

    public static POINT GetCursorPosition()
    {
        GetCursorPos(out var pt);
        return pt;
    }

    public static RECT GetWindowBounds(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        GetWindowRect(handle, out var rect);
        return rect;
    }

    private static bool IsWindows11OrLater()
    {
        var v = Environment.OSVersion.Version;
        return v.Major > 10 || (v.Major == 10 && v.Build >= 22000);
    }
}
