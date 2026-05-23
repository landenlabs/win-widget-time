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

    /// <summary>Moves an embedded (child) window using physical pixel coordinates.</summary>
    public static void MoveEmbeddedWindow(Window window, int x, int y)
    {
        var handle = new WindowInteropHelper(window).Handle;
        GetWindowRect(handle, out var rect);
        MoveWindow(handle, x, y, rect.Right - rect.Left, rect.Bottom - rect.Top, true);
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
