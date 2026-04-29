// Copyright (c) 2026 LanDen Labs - Dennis Lang
namespace WinWidgetTime.Models;

public class WidgetSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Widget";
    public List<PlaceEntry> Places { get; set; } = new();
    public int FontSize { get; set; } = 18;
    public double PositionX { get; set; } = 50;
    public double PositionY { get; set; } = 200;
    public Dictionary<string, ScreenPosition> MonitorPositions { get; set; } = new();
    public bool EmbedInWallpaper { get; set; } = true;
    public string DateFormat { get; set; } = "ddd MMM dd";
    public string TimeFormat { get; set; } = "hh:mm:ss tt";
    public string BackgroundColor { get; set; } = "#000000";
    public double BackgroundOpacity { get; set; } = 0.80;
    public bool ShowTitleBar { get; set; } = true;
}
