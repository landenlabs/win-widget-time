// Copyright (c) 2026 LanDen Labs - Dennis Lang
namespace WinWidgetTime.Models;

public class ScreenPosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class AppSettings
{
    public List<WidgetSettings> Widgets { get; set; } = new();
    public bool AutoStart { get; set; } = false;
}
