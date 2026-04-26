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
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? Defaults();
        }
        catch { return Defaults(); }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDir);
        File.WriteAllText(SettingsFile, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static AppSettings Defaults() => new()
    {
        Places =
        [
            new() { CityName = "New York",    TimeZoneId = "America/New_York",  Label = "New York", Color = "#00FF88" },
            new() { CityName = "London",       TimeZoneId = "Europe/London",     Label = "London",   Color = "#88AAFF" },
            new() { CityName = "Tokyo",        TimeZoneId = "Asia/Tokyo",        Label = "Tokyo",    Color = "#FFAA44" },
        ],
        FontSize = 18,
        PositionX = 50,
        PositionY = 200
    };
}
