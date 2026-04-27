// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.Windows.Forms;
using WinWidgetTime.Models;

namespace WinWidgetTime.Services;

public static class MonitorService
{
    /// <summary>
    /// Returns a stable string key representing the current monitor layout.
    /// Different monitor configs (e.g. laptop-only vs docked) produce different keys.
    /// </summary>
    public static string GetFingerprint()
    {
        var parts = Screen.AllScreens
            .OrderBy(s => s.Bounds.Left).ThenBy(s => s.Bounds.Top)
            .Select(s => $"{s.Bounds.Width}x{s.Bounds.Height}@{s.Bounds.Left},{s.Bounds.Top}");
        return string.Join("|", parts);
    }

    /// <summary>
    /// Returns the saved position for the current monitor config, falling back to
    /// the legacy PositionX/Y if no per-monitor entry exists.
    /// </summary>
    public static (double X, double Y) GetPosition(AppSettings settings)
    {
        var key = GetFingerprint();
        if (settings.MonitorPositions.TryGetValue(key, out var pos))
            return (pos.X, pos.Y);
        return (settings.PositionX, settings.PositionY);
    }

    /// <summary>
    /// Returns (x, y) unchanged if the widget's top-left region overlaps any screen's
    /// working area; otherwise snaps to a safe position on the primary screen.
    /// </summary>
    public static (double X, double Y) ClampToScreen(double x, double y)
    {
        // Use a 60×30 probe area from the top-left corner so at least a grabbable
        // portion must be visible before we consider the position valid.
        var probe = new System.Drawing.Rectangle((int)x, (int)y, 60, 30);
        bool visible = Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(probe));
        if (visible) return (x, y);

        var primary = Screen.PrimaryScreen ?? Screen.AllScreens[0];
        return (primary.WorkingArea.Left + 50, primary.WorkingArea.Top + 200);
    }

    /// <summary>
    /// Persists (x, y) to both the legacy fields and the per-monitor dictionary.
    /// Caller is responsible for calling SettingsService.Save afterwards.
    /// </summary>
    public static void SavePosition(AppSettings settings, double x, double y)
    {
        settings.PositionX = x;
        settings.PositionY = y;
        settings.MonitorPositions[GetFingerprint()] = new ScreenPosition { X = x, Y = y };
    }
}
