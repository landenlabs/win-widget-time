// Copyright (c) 2026 LanDen Labs - Dennis Lang
//
// Pure geometry for widget drag / resize / on-screen recovery / monitor fingerprinting.
// No WPF or WinForms types here, so the unit-test project can compile this file directly.
// The same file (namespace aside) is shared by every win-widget-* project — keep them in sync.
//
// Unit conventions:
//   * Positions (X, Y) are always SCREEN PHYSICAL PIXELS (what GetCursorPos/GetWindowRect use).
//   * Sizes (Width, Height) are WPF device-independent pixels (DIPs).
//   * dpiX / dpiY are the DIP→pixel scale factors (1.0 = 100 %, 1.5 = 150 %).

namespace WinWidgetTime.Services;

public readonly record struct PxRect(int X, int Y, int Width, int Height)
{
    public int Right  => X + Width;
    public int Bottom => Y + Height;
}

public readonly record struct ScreenInfo(PxRect Bounds, PxRect WorkArea, bool IsPrimary);

public enum ResizeZone { None, TopLeft, TopRight, BottomLeft, BottomRight }

public readonly record struct ResizeResult(double Width, double Height, int X, int Y);

public static class WidgetGeometry
{
    /// <summary>Default size of the square corner hot-spot used for resizing, in DIPs.</summary>
    public const double DefaultCornerSize = 16;

    /// <summary>How much of a widget (in pixels, both axes) must overlap a screen to count as visible.</summary>
    public const int DefaultMinVisible = 50;

    /// <summary>Prefix for monitor-layout keys; keys without it are from older versions and are discarded.</summary>
    public const string FingerprintPrefix = "MON:";

    // ── Hit testing ──────────────────────────────────────────────────────────

    /// <summary>
    /// Which resize corner (if any) the point lies in. <paramref name="x"/>/<paramref name="y"/> are
    /// relative to the widget's top-left, in DIPs. Bottom corners always resize; top corners only when
    /// <paramref name="allowTopCorners"/> is true (when a title bar is shown the top is the drag zone).
    /// </summary>
    public static ResizeZone HitTest(double x, double y, double width, double height,
                                     bool allowTopCorners, double cornerSize = DefaultCornerSize)
    {
        if (width <= 0 || height <= 0) return ResizeZone.None;
        if (x < 0 || y < 0 || x > width || y > height) return ResizeZone.None;

        bool left   = x <= cornerSize;
        bool right  = x >= width - cornerSize;
        bool top    = y <= cornerSize;
        bool bottom = y >= height - cornerSize;

        if (bottom && right) return ResizeZone.BottomRight;
        if (bottom && left)  return ResizeZone.BottomLeft;
        if (!allowTopCorners) return ResizeZone.None;
        if (top && right) return ResizeZone.TopRight;
        if (top && left)  return ResizeZone.TopLeft;
        return ResizeZone.None;
    }

    public static bool IsWest(ResizeZone z)  => z is ResizeZone.TopLeft or ResizeZone.BottomLeft;
    public static bool IsNorth(ResizeZone z) => z is ResizeZone.TopLeft or ResizeZone.TopRight;

    // ── Drag ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// New window top-left (pixels) for a drag: the window moves by exactly the cursor delta,
    /// so the grab point stays under the cursor regardless of DPI or embedding.
    /// </summary>
    public static (int X, int Y) DragTo(int startWindowX, int startWindowY,
                                        int startCursorX, int startCursorY,
                                        int cursorX, int cursorY)
        => (startWindowX + (cursorX - startCursorX), startWindowY + (cursorY - startCursorY));

    // ── Resize ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes the new size (DIPs) and top-left (pixels) for a corner resize. The corner opposite
    /// the dragged one stays fixed. <paramref name="dxPx"/>/<paramref name="dyPx"/> are the cursor
    /// movement since the resize started, in pixels.
    /// </summary>
    public static ResizeResult Resize(ResizeZone zone,
                                      double startWidth, double startHeight,
                                      int startX, int startY,
                                      int dxPx, int dyPx,
                                      double dpiX, double dpiY,
                                      double minWidth, double minHeight,
                                      double maxWidth = double.PositiveInfinity,
                                      double maxHeight = double.PositiveInfinity)
    {
        if (zone == ResizeZone.None)
            return new ResizeResult(startWidth, startHeight, startX, startY);

        dpiX = dpiX > 0 ? dpiX : 1.0;
        dpiY = dpiY > 0 ? dpiY : 1.0;
        maxWidth  = Math.Max(minWidth, maxWidth);
        maxHeight = Math.Max(minHeight, maxHeight);

        bool west  = IsWest(zone);
        bool north = IsNorth(zone);

        double dxDip = dxPx / dpiX;
        double dyDip = dyPx / dpiY;

        double newW = Math.Clamp(startWidth  + (west  ? -dxDip : dxDip), minWidth,  maxWidth);
        double newH = Math.Clamp(startHeight + (north ? -dyDip : dyDip), minHeight, maxHeight);

        // Keep the opposite edge anchored: moving the west/north edge shifts the origin by the size change.
        int x = west  ? startX + (int)Math.Round((startWidth  - newW) * dpiX) : startX;
        int y = north ? startY + (int)Math.Round((startHeight - newH) * dpiY) : startY;

        return new ResizeResult(newW, newH, x, y);
    }

    /// <summary>
    /// For content-sized widgets (text that sizes to content), a corner drag scales the content
    /// instead of setting an explicit size. Returns the new scale percent, proportional to how much
    /// the dragged corner grew/shrank the widget (average of the width and height ratios).
    /// </summary>
    public static int ScalePercentFromResize(int startPercent, double startWidth, double startHeight,
                                             double newWidth, double newHeight,
                                             int minPercent = 50, int maxPercent = 200)
    {
        if (startWidth <= 0 || startHeight <= 0) return Math.Clamp(startPercent, minPercent, maxPercent);
        double ratio = (newWidth / startWidth + newHeight / startHeight) / 2.0;
        return Math.Clamp((int)Math.Round(startPercent * ratio), minPercent, maxPercent);
    }

    // ── On-screen recovery ───────────────────────────────────────────────────

    /// <summary>True when at least <paramref name="minVisible"/> pixels of the window overlap some screen on both axes.</summary>
    public static bool IsVisible(PxRect window, IReadOnlyList<ScreenInfo> screens, int minVisible = DefaultMinVisible)
    {
        foreach (var s in screens)
        {
            int overlapW = Math.Min(window.Right,  s.Bounds.Right)  - Math.Max(window.X, s.Bounds.X);
            int overlapH = Math.Min(window.Bottom, s.Bounds.Bottom) - Math.Max(window.Y, s.Bounds.Y);
            int needW = Math.Min(minVisible, Math.Max(1, window.Width));
            int needH = Math.Min(minVisible, Math.Max(1, window.Height));
            if (overlapW >= needW && overlapH >= needH) return true;
        }
        return false;
    }

    /// <summary>
    /// If the window is (effectively) off every screen — e.g. its monitor was unplugged — returns a new
    /// top-left that places it fully inside the work area of the screen nearest to where it was.
    /// Returns null when the window is already visible and should stay where it is.
    /// </summary>
    public static (int X, int Y)? EnsureVisible(PxRect window, IReadOnlyList<ScreenInfo> screens,
                                                int minVisible = DefaultMinVisible, int margin = 20)
    {
        if (screens.Count == 0) return null;
        if (IsVisible(window, screens, minVisible)) return null;

        var target = NearestScreen(window, screens);
        var area = target.WorkArea.Width > 0 && target.WorkArea.Height > 0 ? target.WorkArea : target.Bounds;
        return ClampInto(window, area, margin);
    }

    /// <summary>Clamps the window's top-left so the whole window sits inside <paramref name="area"/> (inset by margin when it fits).</summary>
    public static (int X, int Y) ClampInto(PxRect window, PxRect area, int margin = 0)
    {
        int mx = window.Width  + 2 * margin <= area.Width  ? margin : 0;
        int my = window.Height + 2 * margin <= area.Height ? margin : 0;

        int minX = area.X + mx, maxX = area.Right  - window.Width  - mx;
        int minY = area.Y + my, maxY = area.Bottom - window.Height - my;

        int x = maxX < minX ? area.X : Math.Clamp(window.X, minX, maxX);
        int y = maxY < minY ? area.Y : Math.Clamp(window.Y, minY, maxY);
        return (x, y);
    }

    private static ScreenInfo NearestScreen(PxRect window, IReadOnlyList<ScreenInfo> screens)
    {
        double cx = window.X + window.Width / 2.0;
        double cy = window.Y + window.Height / 2.0;

        ScreenInfo best = screens[0];
        double bestDist = double.MaxValue;
        foreach (var s in screens)
        {
            // Distance from the window centre to the nearest point of the screen rectangle.
            double dx = Math.Max(Math.Max(s.Bounds.X - cx, 0), cx - s.Bounds.Right);
            double dy = Math.Max(Math.Max(s.Bounds.Y - cy, 0), cy - s.Bounds.Bottom);
            double d = dx * dx + dy * dy;
            if (d < bestDist || (d == bestDist && s.IsPrimary))
            {
                best = s;
                bestDist = d;
            }
        }
        return best;
    }

    // ── Monitor layout fingerprint ───────────────────────────────────────────

    /// <summary>
    /// Deterministic, human-readable key for the current monitor layout, e.g.
    /// "MON:0,0,2560x1440,P|2560,0,1920x1080". Stable across runs (unlike string.GetHashCode),
    /// independent of enumeration order.
    /// </summary>
    public static string Fingerprint(IReadOnlyList<ScreenInfo> screens)
    {
        if (screens.Count == 0) return FingerprintPrefix + "none";
        var parts = screens
            .OrderBy(s => s.Bounds.X).ThenBy(s => s.Bounds.Y)
            .Select(s => $"{s.Bounds.X},{s.Bounds.Y},{s.Bounds.Width}x{s.Bounds.Height}{(s.IsPrimary ? ",P" : "")}");
        return FingerprintPrefix + string.Join("|", parts);
    }
}
