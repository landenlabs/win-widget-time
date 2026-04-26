namespace WinWidgetTime.Models;

public class AppSettings
{
    public List<PlaceEntry> Places { get; set; } = new();
    public int FontSize { get; set; } = 18;
    public double PositionX { get; set; } = 50;
    public double PositionY { get; set; } = 200;
    public bool AutoStart { get; set; } = false;
    public bool EmbedInWallpaper { get; set; } = true;
    public string DateFormat { get; set; } = "ddd MMM dd";
    public string TimeFormat { get; set; } = "hh:mm:ss tt";
}
