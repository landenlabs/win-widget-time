// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.Windows.Forms;
using WinWidgetTime.Models;

namespace WinWidgetTime.Services;

/// <summary>
/// Monitor enumeration and per-monitor-layout widget positions.
/// All positions are screen physical pixels (see <see cref="WidgetGeometry"/>).
/// </summary>
public static class MonitorService
{
    /// <summary>All attached screens (bounds + work area in physical pixels).</summary>
    public static List<ScreenInfo> GetScreens()
    {
        var list = new List<ScreenInfo>();
        try
        {
            foreach (var s in Screen.AllScreens)
            {
                list.Add(new ScreenInfo(
                    new PxRect(s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height),
                    new PxRect(s.WorkingArea.X, s.WorkingArea.Y, s.WorkingArea.Width, s.WorkingArea.Height),
                    s.Primary));
            }
        }
        catch
        {
            // Fall through to the default below.
        }

        if (list.Count == 0)
            list.Add(new ScreenInfo(new PxRect(0, 0, 1920, 1080), new PxRect(0, 0, 1920, 1040), true));
        return list;
    }

    /// <summary>
    /// Stable, readable key for the current monitor layout (a "MON:" fingerprint).
    /// Different monitor configs (e.g. laptop-only vs docked) produce different keys.
    /// </summary>
    public static string GetFingerprint() => WidgetGeometry.Fingerprint(GetScreens());

    /// <summary>
    /// Key format written by versions before the shared WidgetGeometry fingerprint
    /// ("WxH@L,T|..."). Still read so existing users keep their per-layout positions.
    /// </summary>
    private static string LegacyFingerprint(IReadOnlyList<ScreenInfo> screens) =>
        string.Join("|", screens
            .OrderBy(s => s.Bounds.X).ThenBy(s => s.Bounds.Y)
            .Select(s => $"{s.Bounds.Width}x{s.Bounds.Height}@{s.Bounds.X},{s.Bounds.Y}"));

    /// <summary>
    /// Saved top-left (screen pixels) for the current monitor layout; falls back to the
    /// legacy-format key, then to the last position saved on any layout.
    /// </summary>
    public static (int X, int Y) GetPosition(WidgetSettings widget)
    {
        var screens = GetScreens();
        if (widget.MonitorPositions.TryGetValue(WidgetGeometry.Fingerprint(screens), out var pos) ||
            widget.MonitorPositions.TryGetValue(LegacyFingerprint(screens), out pos))
            return (Round(pos.X), Round(pos.Y));
        return (Round(widget.PositionX), Round(widget.PositionY));
    }

    /// <summary>
    /// Persists the top-left (screen pixels) to both the fallback fields and the per-layout
    /// dictionary. Caller is responsible for calling SettingsService.Save afterwards.
    /// </summary>
    public static void SavePosition(WidgetSettings widget, int x, int y)
    {
        widget.PositionX = x;
        widget.PositionY = y;
        widget.MonitorPositions[GetFingerprint()] = new ScreenPosition { X = x, Y = y };
    }

    private static int Round(double v) =>
        double.IsFinite(v) ? (int)Math.Round(Math.Clamp(v, int.MinValue / 2.0, int.MaxValue / 2.0)) : 0;
}
