using System.Net.Http;
using System.Text.Json;
using GeoTimeZone;
using TimeZoneConverter;

namespace WinWidgetTime.Services;

public record GeocodingResult(string IanaTimeZoneId, string DisplayName, double Latitude, double Longitude);

public static class GeocodingService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    static GeocodingService()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("WinWidgetTime/1.0 (desktop timezone widget)");
    }

    public static async Task<GeocodingResult?> ResolveAsync(string cityName)
    {
        try
        {
            var url = $"https://nominatim.openstreetmap.org/search" +
                      $"?q={Uri.EscapeDataString(cityName)}&format=json&limit=1";

            var json = await Http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.GetArrayLength() == 0) return null;

            var item = root[0];
            double lat = double.Parse(item.GetProperty("lat").GetString()!,
                System.Globalization.CultureInfo.InvariantCulture);
            double lon = double.Parse(item.GetProperty("lon").GetString()!,
                System.Globalization.CultureInfo.InvariantCulture);
            string displayName = item.GetProperty("display_name").GetString()!;

            var tzResult = TimeZoneLookup.GetTimeZone(lat, lon);
            return new GeocodingResult(tzResult.Result, displayName, lat, lon);
        }
        catch { return null; }
    }

    public static string GetUtcOffsetLabel(string ianaTimeZoneId)
    {
        try
        {
            var tzi = TZConvert.GetTimeZoneInfo(ianaTimeZoneId);
            var offset = tzi.GetUtcOffset(DateTimeOffset.UtcNow);
            string sign = offset >= TimeSpan.Zero ? "+" : "-";
            return $"{ianaTimeZoneId}  (UTC{sign}{Math.Abs(offset.Hours):D2}:{Math.Abs(offset.Minutes):D2})";
        }
        catch { return ianaTimeZoneId; }
    }

    public static DateTimeOffset GetLocalTime(string ianaTimeZoneId)
    {
        try
        {
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TZConvert.GetTimeZoneInfo(ianaTimeZoneId));
        }
        catch { return DateTimeOffset.UtcNow; }
    }
}
