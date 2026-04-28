// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.IO;
using System.Text.Json;
using WinWidgetTime.Models;

namespace WinWidgetTime.Services;

public static class SettingsService
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinWidgetTime");

    private static readonly string SettingsFile =
        Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFile)) return Defaults();
            var json = File.ReadAllText(SettingsFile);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? Defaults();

            // One-time migration: old format had places/fontSize/etc. at root level
            if (settings.Widgets.Count == 0)
            {
                var legacy = JsonSerializer.Deserialize<LegacyAppSettings>(json, JsonOptions);
                if (legacy?.Places?.Count > 0)
                {
                    settings.Widgets.Add(new WidgetSettings
                    {
                        Name              = "Widget 1",
                        Places            = legacy.Places,
                        FontSize          = legacy.FontSize,
                        PositionX         = legacy.PositionX,
                        PositionY         = legacy.PositionY,
                        MonitorPositions  = legacy.MonitorPositions ?? new(),
                        EmbedInWallpaper  = legacy.EmbedInWallpaper,
                        DateFormat        = legacy.DateFormat,
                        TimeFormat        = legacy.TimeFormat,
                        BackgroundColor   = legacy.BackgroundColor,
                        BackgroundOpacity = legacy.BackgroundOpacity,
                    });
                    settings.AutoStart = legacy.AutoStart;
                }
            }

            if (settings.Widgets.Count == 0)
                settings.Widgets.Add(DefaultWidget());

            return settings;
        }
        catch { return Defaults(); }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDir);
        File.WriteAllText(SettingsFile, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public static WidgetSettings DefaultWidget() => new()
    {
        Name = "Widget 1",
        Places =
        [
            new() { CityName = "New York", TimeZoneId = "America/New_York", Label = "New York", Color = "#00FF88" },
            new() { CityName = "London",   TimeZoneId = "Europe/London",    Label = "London",   Color = "#88AAFF" },
            new() { CityName = "Tokyo",    TimeZoneId = "Asia/Tokyo",       Label = "Tokyo",    Color = "#FFAA44" },
        ],
        FontSize = 18,
        PositionX = 50,
        PositionY = 200,
    };

    private static AppSettings Defaults() => new()
    {
        Widgets = [DefaultWidget()],
    };

    // Used only during one-time migration from pre-multi-widget settings files
    private sealed class LegacyAppSettings
    {
        public List<PlaceEntry>? Places { get; set; }
        public int FontSize { get; set; } = 18;
        public double PositionX { get; set; } = 50;
        public double PositionY { get; set; } = 200;
        public Dictionary<string, ScreenPosition>? MonitorPositions { get; set; }
        public bool AutoStart { get; set; } = false;
        public bool EmbedInWallpaper { get; set; } = true;
        public string DateFormat { get; set; } = "ddd MMM dd";
        public string TimeFormat { get; set; } = "hh:mm:ss tt";
        public string BackgroundColor { get; set; } = "#000000";
        public double BackgroundOpacity { get; set; } = 0.80;
    }
}
