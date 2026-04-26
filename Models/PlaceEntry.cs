using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace WinWidgetTime.Models;

public class PlaceEntry : INotifyPropertyChanged
{
    private string _cityName = "";
    private string _timeZoneId = "UTC";
    private string _label = "";
    private string _color = "#00FF88";
    private SolidColorBrush? _colorBrush;

    public string CityName
    {
        get => _cityName;
        set { _cityName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    public string TimeZoneId
    {
        get => _timeZoneId;
        set { _timeZoneId = value; OnPropertyChanged(); }
    }

    public string Label
    {
        get => _label;
        set { _label = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    [JsonIgnore]
    public string DisplayName => string.IsNullOrEmpty(_label) ? _cityName : _label;

    public string Color
    {
        get => _color;
        set
        {
            _color = value;
            _colorBrush = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ColorBrush));
        }
    }

    [JsonIgnore]
    public SolidColorBrush ColorBrush => _colorBrush ??= ParseBrush();

    private SolidColorBrush ParseBrush()
    {
        try { return new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(_color)); }
        catch { return Brushes.White; }
    }

    public PlaceEntry Clone() => new()
    {
        CityName = CityName,
        TimeZoneId = TimeZoneId,
        Label = Label,
        Color = Color
    };

    public override string ToString() => string.IsNullOrEmpty(Label) ? CityName : Label;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
