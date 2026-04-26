using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WinWidgetTime.Models;
using WinWidgetTime.Services;

namespace WinWidgetTime.Windows;

public partial class SettingsWindow : Window, INotifyPropertyChanged
{
    // ── Bindable properties ──────────────────────────────────────────────────

    public ObservableCollection<PlaceEntry> Places { get; } = [];

    private PlaceEntry? _selectedPlace;
    public PlaceEntry? SelectedPlace
    {
        get => _selectedPlace;
        set
        {
            _selectedPlace = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedPlace));
            RefreshEditPanel();
        }
    }

    public bool HasSelectedPlace => _selectedPlace != null;

    private int _fontSizeValue;
    public int FontSizeValue
    {
        get => _fontSizeValue;
        set { _fontSizeValue = value; OnPropertyChanged(); }
    }

    private bool _autoStartEnabled;
    public bool AutoStartEnabled
    {
        get => _autoStartEnabled;
        set { _autoStartEnabled = value; OnPropertyChanged(); }
    }

    private bool _embedInWallpaper;
    public bool EmbedInWallpaper
    {
        get => _embedInWallpaper;
        set { _embedInWallpaper = value; OnPropertyChanged(); }
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    public SettingsWindow()
    {
        InitializeComponent();
        Topmost = true;

        // Load working copies
        foreach (var p in App.Settings.Places)
            Places.Add(p.Clone());

        FontSizeValue = App.Settings.FontSize;
        AutoStartEnabled = AutoStartService.IsEnabled();
        EmbedInWallpaper = App.Settings.EmbedInWallpaper;

        // Subscribe to SelectedPlace.PropertyChanged to keep edit panel in sync
        Places.CollectionChanged += (_, _) => { };

        if (Places.Count > 0)
            SelectedPlace = Places[0];
    }

    // ── Edit panel sync ──────────────────────────────────────────────────────

    private void RefreshEditPanel()
    {
        if (_selectedPlace == null)
        {
            LookupStatus.Text = "";
            TzOffsetLabel.Text = "";
            ColorHexLabel.Text = "";
            return;
        }

        _selectedPlace.PropertyChanged -= SelectedPlace_PropertyChanged;
        _selectedPlace.PropertyChanged += SelectedPlace_PropertyChanged;

        UpdateTzOffsetLabel();
        UpdateColorHexLabel();
    }

    private void SelectedPlace_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlaceEntry.TimeZoneId))
            UpdateTzOffsetLabel();
        if (e.PropertyName == nameof(PlaceEntry.Color))
            UpdateColorHexLabel();
    }

    private void UpdateTzOffsetLabel()
    {
        if (_selectedPlace == null) return;
        try { TzOffsetLabel.Text = GeocodingService.GetUtcOffsetLabel(_selectedPlace.TimeZoneId); }
        catch { TzOffsetLabel.Text = ""; }
    }

    private void UpdateColorHexLabel()
    {
        if (_selectedPlace == null) return;
        ColorHexLabel.Text = _selectedPlace.Color.ToUpperInvariant();
    }

    // ── Add / Delete ─────────────────────────────────────────────────────────

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var entry = new PlaceEntry
        {
            CityName = "New City",
            Label    = "New City",
            TimeZoneId = "UTC",
            Color    = "#FFFFFF"
        };
        Places.Add(entry);
        SelectedPlace = entry;
        PlacesList.ScrollIntoView(entry);
        CityBox.Focus();
        CityBox.SelectAll();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPlace == null) return;
        var idx = Places.IndexOf(_selectedPlace);
        Places.Remove(_selectedPlace);
        SelectedPlace = Places.Count > 0
            ? Places[Math.Min(idx, Places.Count - 1)]
            : null;
    }

    // ── Geocode lookup ───────────────────────────────────────────────────────

    private async void Lookup_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPlace == null) return;
        await DoLookup(_selectedPlace.CityName);
    }

    private async void CityBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _selectedPlace != null)
            await DoLookup(_selectedPlace.CityName);
    }

    private async Task DoLookup(string cityName)
    {
        if (string.IsNullOrWhiteSpace(cityName)) return;

        LookupBtn.IsEnabled = false;
        LookupStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xA6, 0xAD, 0xC8));
        LookupStatus.Text = $"Looking up \"{cityName}\"…";

        var result = await GeocodingService.ResolveAsync(cityName);

        LookupBtn.IsEnabled = true;

        if (result == null)
        {
            LookupStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xF3, 0x8B, 0xA8));
            LookupStatus.Text = "Not found. Check spelling or try a larger city.";
            return;
        }

        if (_selectedPlace == null) return;

        _selectedPlace.TimeZoneId = result.IanaTimeZoneId;

        // Auto-fill label if it's still the default
        if (string.IsNullOrWhiteSpace(_selectedPlace.Label) || _selectedPlace.Label == "New City")
            _selectedPlace.Label = _selectedPlace.CityName;

        LookupStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1));
        LookupStatus.Text = $"✓  {result.DisplayName.Split(',')[0].Trim()}";
    }

    // ── Color picker ─────────────────────────────────────────────────────────

    private void ColorSwatch_Click(object sender, RoutedEventArgs e) => OpenColorPicker();
    private void ColorSwatch_Click(object sender, MouseButtonEventArgs e) => OpenColorPicker();

    private void OpenColorPicker()
    {
        if (_selectedPlace == null) return;

        var picker = new ColorPickerWindow(_selectedPlace.Color) { Owner = this };
        if (picker.ShowDialog() == true)
        {
            var c = picker.SelectedColor;
            _selectedPlace.Color = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    // ── Drag-to-reorder ──────────────────────────────────────────────────────

    private PlaceEntry? _pendingDrag;
    private PlaceEntry? _dragItem;
    private Point _dragStart;

    private void PlacesList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Walk up from the click source to find a drag handle element
        var element = e.OriginalSource as DependencyObject;
        while (element != null && element != PlacesList)
        {
            if (element is FrameworkElement fe && fe.Tag as string == "DragHandle"
                && fe.DataContext is PlaceEntry entry)
            {
                _pendingDrag = entry;
                _dragStart = e.GetPosition(null);
                return;
            }
            element = VisualTreeHelper.GetParent(element);
        }
        _pendingDrag = null;
    }

    private void PlacesList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_pendingDrag == null || e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(null);
        var diff = pos - _dragStart;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        var item = _pendingDrag;
        _pendingDrag = null;
        _dragItem = item;
        DragDrop.DoDragDrop(PlacesList, item, DragDropEffects.Move);
        _dragItem = null;
    }

    private void PlacesList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        => _pendingDrag = null;

    private void PlacesList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = _dragItem != null ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void PlacesList_Drop(object sender, DragEventArgs e)
    {
        if (_dragItem == null) return;

        var target = GetPlaceAtDropPoint(e.GetPosition(PlacesList));
        if (target != null && target != _dragItem)
        {
            int from = Places.IndexOf(_dragItem);
            int to = Places.IndexOf(target);
            if (from >= 0 && to >= 0)
                Places.Move(from, to);
        }
        SelectedPlace = _dragItem;
        _dragItem = null;
    }

    private PlaceEntry? GetPlaceAtDropPoint(Point point)
    {
        var element = PlacesList.InputHitTest(point) as DependencyObject;
        while (element != null)
        {
            if (element is ListBoxItem lbi && lbi.Content is PlaceEntry place)
                return place;
            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }

    // ── Save / Cancel ────────────────────────────────────────────────────────

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        App.Settings.Places = [.. Places];
        App.Settings.FontSize = FontSizeValue;
        App.Settings.EmbedInWallpaper = EmbedInWallpaper;

        AutoStartService.SetEnabled(AutoStartEnabled);
        App.Settings.AutoStart = AutoStartEnabled;

        SettingsService.Save(App.Settings);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ── INotifyPropertyChanged ───────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
