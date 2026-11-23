// Copyright (c) 2026 LanDen Labs - Dennis Lang
namespace WinWidgetTime.Models;

public class WidgetSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Widget";
    public List<PlaceEntry> Places { get; set; } = new();
    public int FontSize { get; set; } = 18;
    /// <summary>Last saved top-left, in screen physical pixels (fallback when the layout has no entry).</summary>
    public double PositionX { get; set; } = 50;
    public double PositionY { get; set; } = 200;

    /// <summary>
    /// Top-left (screen pixels) per monitor layout. Key: <see cref="Services.MonitorService.GetFingerprint"/>
    /// (a "MON:" fingerprint; older "WxH@L,T|..." keys are still read).
    /// </summary>
    public Dictionary<string, ScreenPosition> MonitorPositions { get; set; } = new();

    /// <summary>Embed in the desktop wallpaper layer (Windows 11). Takes effect on next launch.</summary>
    public bool EmbedInWallpaper { get; set; } = true;
    public string DateFormat { get; set; } = "ddd MMM dd";
    public string TimeFormat { get; set; } = "hh:mm:ss tt";
    private string _dateTimeFormat = "";
    public string DateTimeFormat
    {
        get => string.IsNullOrEmpty(_dateTimeFormat) ? DateFormat + "  " + TimeFormat : _dateTimeFormat;
        set => _dateTimeFormat = value;
    }
    public string BackgroundColor { get; set; } = "#000000";
    public double BackgroundOpacity { get; set; } = 0.80;
    public bool ShowTitleBar { get; set; } = true;
}
