// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using TimeZoneConverter;
using WinWidgetTime.Models;

namespace WinWidgetTime.ViewModels;

public class TimeDisplayItem : INotifyPropertyChanged
{
    private readonly PlaceEntry _entry;
    private string _datePart = "";
    private string _timePart = "";
    private SolidColorBrush? _colorBrush;

    public TimeDisplayItem(PlaceEntry entry)
    {
        _entry = entry;
        Update();
    }

    public string Label => _entry.Label.Length > 0 ? _entry.Label : _entry.CityName;

    public SolidColorBrush ColorBrush
    {
        get
        {
            if (_colorBrush == null)
            {
                try { _colorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_entry.Color)); }
                catch { _colorBrush = Brushes.White; }
            }
            return _colorBrush;
        }
    }

    public string DatePart
    {
        get => _datePart;
        private set { _datePart = value; OnPropertyChanged(); }
    }

    public string TimePart
    {
        get => _timePart;
        private set { _timePart = value; OnPropertyChanged(); }
    }

    public void Update(string dateFormat = "ddd MMM dd", string timeFormat = "hh:mm:ss tt")
    {
        // Invalidate color brush in case settings changed
        _colorBrush = null;
        OnPropertyChanged(nameof(ColorBrush));
        OnPropertyChanged(nameof(Label));

        try
        {
            var tzi = string.Equals(_entry.TimeZoneId, "Local", StringComparison.OrdinalIgnoreCase)
                ? TimeZoneInfo.Local
                : TZConvert.GetTimeZoneInfo(_entry.TimeZoneId);
            var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tzi);
            if (!string.IsNullOrWhiteSpace(_entry.DateTimeFormat))
            {
                DatePart = "";
                TimePart = now.ToString(_entry.DateTimeFormat);
            }
            else
            {
                DatePart = now.ToString(dateFormat);
                TimePart = now.ToString(timeFormat);
            }
        }
        catch
        {
            var now = DateTimeOffset.UtcNow;
            DatePart = now.ToString("ddd MMM dd");
            TimePart = now.ToString("hh:mm:ss tt");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
