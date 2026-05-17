// Copyright (c) 2026 LanDen Labs - Dennis Lang
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
    private readonly WidgetSettings _widget;
    private readonly WidgetWindow? _livePreviewTarget;

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

    private string _widgetName = "";
    public string WidgetName
    {
        get => _widgetName;
        set { _widgetName = value; OnPropertyChanged(); }
    }

    private int _fontSizeValue;
    public int FontSizeValue
    {
        get => _fontSizeValue;
        set { _fontSizeValue = value; OnPropertyChanged(); _livePreviewTarget?.ApplyFontSize(value); }
    }

    private bool _autoStartEnabled;
    public bool AutoStartEnabled
    {
        get => _autoStartEnabled;
        set { _autoStartEnabled = value; OnPropertyChanged(); }
    }

    private bool _showTitleBar;
    public bool ShowTitleBar
    {
        get => _showTitleBar;
        set { _showTitleBar = value; OnPropertyChanged(); }
    }

    private string _globalFormat = "ddd MMM dd  hh:mm:ss tt";

    private string _bgColorHex = "#000000";
    public string BgColorHex
    {
        get => _bgColorHex;
        set
        {
            _bgColorHex = value;
            _bgColorBrush = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BgColorBrush));
            LivePreviewBackground();
        }
    }

    private SolidColorBrush? _bgColorBrush;
    public SolidColorBrush BgColorBrush => _bgColorBrush ??= ParseBgBrush();

    private SolidColorBrush ParseBgBrush()
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(_bgColorHex)); }
        catch { return Brushes.Black; }
    }

    private int _bgOpacityPercent;
    public int BgOpacityPercent
    {
        get => _bgOpacityPercent;
        set { _bgOpacityPercent = value; OnPropertyChanged(); LivePreviewBackground(); }
    }

    // ── Originals for Cancel restore ────────────────────────────────────────

    private readonly int _origFontSize;
    private readonly string _origBgColor;
    private readonly int _origBgOpacityPercent;
    private readonly double _origPosX;
    private readonly double _origPosY;

    // ── Position picker ──────────────────────────────────────────────────────

    private double _mapScale;
    private double _mapLeft;     // canvas-pixel X of virtual desktop left edge
    private double _mapTop;      // canvas-pixel Y of virtual desktop top edge
    private double _mapOffsetX;  // screen coord of virtual desktop left
    private double _mapOffsetY;  // screen coord of virtual desktop top
    private double _dpiScaleX = 1.0;
    private double _dpiScaleY = 1.0;
    private Border? _widgetMarker;
    private bool _markerDragging;
    private Point _markerDragStart;
    private double _markerDragOrigLeft;
    private double _markerDragOrigTop;
    private double _editPosX;
    private double _editPosY;

    public string WidgetPositionText => $"X: {(int)_editPosX}  Y: {(int)_editPosY}";

    // ── Constructor ──────────────────────────────────────────────────────────

    public SettingsWindow(WidgetSettings widget, WidgetWindow? livePreviewTarget = null)
    {
        _widget = widget;
        _livePreviewTarget = livePreviewTarget;

        InitializeComponent();
        Topmost = true;

        // Snapshot originals so Cancel can restore the live preview
        _origFontSize         = _widget.FontSize;
        _origBgColor          = _widget.BackgroundColor;
        _origBgOpacityPercent = (int)Math.Round(_widget.BackgroundOpacity * 100);
        _origPosX = _livePreviewTarget?.Left ?? _widget.PositionX;
        _origPosY = _livePreviewTarget?.Top  ?? _widget.PositionY;
        _editPosX = _origPosX;
        _editPosY = _origPosY;

        // Load working copies
        foreach (var p in _widget.Places)
            Places.Add(p.Clone());

        WidgetName        = _widget.Name;
        FontSizeValue     = _widget.FontSize;
        _globalFormat     = _widget.DateTimeFormat;
        AutoStartEnabled  = AutoStartService.IsEnabled();
        ShowTitleBar      = _widget.ShowTitleBar;
        _bgColorHex       = _widget.BackgroundColor;
        _bgOpacityPercent = (int)Math.Round(_widget.BackgroundOpacity * 100);
        OnPropertyChanged(nameof(BgColorHex));
        OnPropertyChanged(nameof(BgColorBrush));
        OnPropertyChanged(nameof(BgOpacityPercent));

        Places.CollectionChanged += (_, _) => { };

        if (Places.Count > 0)
            SelectedPlace = Places[0];

        FormatHelpText.Text =
            "yyyy    4-digit year           2026\n" +
            "yy      2-digit year           26\n" +
            "MMMM    Full month name        January\n" +
            "MMM     3-char month abbr.     Jan\n" +
            "MM      2-digit month          01 – 12\n" +
            "M       Month number           1 – 12\n" +
            "dddd    Full day name          Monday\n" +
            "ddd     3-char day abbr.       Mon\n" +
            "dd      2-digit day            01 – 31\n" +
            "d       Day number             1 – 31\n" +
            "HH      24-hour, padded        00 – 23\n" +
            "H       24-hour                0 – 23\n" +
            "hh      12-hour, padded        01 – 12\n" +
            "h       12-hour                1 – 12\n" +
            "mm      Minutes                00 – 59\n" +
            "ss      Seconds                00 – 59\n" +
            "tt      AM / PM designator     AM · PM\n" +
            "fff     Milliseconds           001 – 999\n" +
            "zzz     UTC offset (+hh:mm)    +05:30\n" +
            "z       UTC offset hours       +5";
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

        if (string.IsNullOrWhiteSpace(_selectedPlace.Label) || _selectedPlace.Label == "New City")
            _selectedPlace.Label = _selectedPlace.CityName;

        LookupStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1));
        LookupStatus.Text = $"✓  {result.DisplayName.Split(',')[0].Trim()}";
    }

    // ── Live preview helpers ─────────────────────────────────────────────────

    private void LivePreviewBackground()
        => _livePreviewTarget?.ApplyBackground(_bgColorHex, _bgOpacityPercent / 100.0);

    // ── Background color picker ──────────────────────────────────────────────

    private void BgColorSwatch_Click(object sender, MouseButtonEventArgs e)
    {
        var picker = new ColorPickerWindow(_bgColorHex) { Owner = this };
        if (picker.ShowDialog() == true)
        {
            var c = picker.SelectedColor;
            BgColorHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    // ── Format help popup ────────────────────────────────────────────────────

    private void FormatHelp_Click(object sender, RoutedEventArgs e)
        => FormatHelpPopup.IsOpen = !FormatHelpPopup.IsOpen;

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

    // ── Screen-map position picker ───────────────────────────────────────────

    private void Window_Loaded(object sender, RoutedEventArgs e) => BuildScreenMap();

    private void BuildScreenMap()
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        int minX = screens.Min(s => s.Bounds.Left);
        int minY = screens.Min(s => s.Bounds.Top);
        int maxX = screens.Max(s => s.Bounds.Right);
        int maxY = screens.Max(s => s.Bounds.Bottom);
        _mapOffsetX = minX;
        _mapOffsetY = minY;

        double cW = ScreenMapCanvas.ActualWidth;
        double cH = ScreenMapCanvas.ActualHeight;
        if (cW <= 0 || cH <= 0) return;

        double vdW = maxX - minX;
        double vdH = maxY - minY;
        _mapScale = Math.Min(cW / vdW, cH / vdH);

        // Center the scaled virtual desktop within the canvas
        _mapLeft = (cW - vdW * _mapScale) / 2.0;
        _mapTop  = (cH - vdH * _mapScale) / 2.0;

        var source = PresentationSource.FromVisual(this);
        _dpiScaleX = source?.CompositionTarget.TransformToDevice.M11 ?? 1.0;
        _dpiScaleY = source?.CompositionTarget.TransformToDevice.M22 ?? 1.0;

        ScreenMapCanvas.Children.Clear();

        foreach (var screen in screens)
        {
            double left = _mapLeft + (screen.Bounds.Left - minX) * _mapScale;
            double top  = _mapTop  + (screen.Bounds.Top  - minY) * _mapScale;
            double w    = screen.Bounds.Width  * _mapScale;
            double h    = screen.Bounds.Height * _mapScale;

            var monitorRect = new Border
            {
                Width = w, Height = h,
                Background       = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x30)),
                BorderBrush      = new SolidColorBrush(Color.FromRgb(0x45, 0x45, 0x70)),
                BorderThickness  = new Thickness(1),
                CornerRadius     = new CornerRadius(2),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(monitorRect, left);
            Canvas.SetTop(monitorRect, top);
            ScreenMapCanvas.Children.Add(monitorRect);

            var lbl = new TextBlock
            {
                Text       = screen.Primary ? "Primary" : $"{screen.Bounds.Width}×{screen.Bounds.Height}",
                FontSize   = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0x58, 0x5B, 0x70)),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(lbl, left + 3);
            Canvas.SetTop(lbl,  top  + 2);
            ScreenMapCanvas.Children.Add(lbl);
        }

        // Widget marker — size in canvas pixels derived from the actual widget dimensions
        double widgetWpx = (_livePreviewTarget?.ActualWidth  ?? 200) * _dpiScaleX;
        double widgetHpx = (_livePreviewTarget?.ActualHeight ?? 120) * _dpiScaleY;
        double markerW   = Math.Max(widgetWpx * _mapScale, 14);
        double markerH   = Math.Max(widgetHpx * _mapScale, 8);

        double markerLeft = _mapLeft + (_editPosX * _dpiScaleX - minX) * _mapScale;
        double markerTop  = _mapTop  + (_editPosY * _dpiScaleY - minY) * _mapScale;

        _widgetMarker = new Border
        {
            Width = markerW, Height = markerH,
            Background      = new SolidColorBrush(Color.FromArgb(0xCC, 0x89, 0xB4, 0xFA)),
            BorderBrush     = Brushes.White,
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(2),
            Cursor          = Cursors.SizeAll,
            ToolTip         = "Drag to reposition the widget"
        };
        _widgetMarker.MouseLeftButtonDown += WidgetMarker_MouseLeftButtonDown;
        _widgetMarker.MouseMove           += WidgetMarker_MouseMove;
        _widgetMarker.MouseLeftButtonUp   += WidgetMarker_MouseLeftButtonUp;

        Canvas.SetLeft(_widgetMarker, markerLeft);
        Canvas.SetTop(_widgetMarker, markerTop);
        System.Windows.Controls.Panel.SetZIndex(_widgetMarker, 10);
        ScreenMapCanvas.Children.Add(_widgetMarker);
    }

    private void WidgetMarker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _markerDragging     = true;
        _markerDragStart    = e.GetPosition(ScreenMapCanvas);
        _markerDragOrigLeft = Canvas.GetLeft(_widgetMarker!);
        _markerDragOrigTop  = Canvas.GetTop(_widgetMarker!);
        _widgetMarker!.CaptureMouse();
        e.Handled = true;
    }

    private void WidgetMarker_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_markerDragging || _widgetMarker == null) return;

        var pos     = e.GetPosition(ScreenMapCanvas);
        double newL = _markerDragOrigLeft + (pos.X - _markerDragStart.X);
        double newT = _markerDragOrigTop  + (pos.Y - _markerDragStart.Y);

        newL = Math.Max(0, Math.Min(newL, ScreenMapCanvas.ActualWidth  - _widgetMarker.Width));
        newT = Math.Max(0, Math.Min(newT, ScreenMapCanvas.ActualHeight - _widgetMarker.Height));

        Canvas.SetLeft(_widgetMarker, newL);
        Canvas.SetTop(_widgetMarker, newT);

        // Canvas coords → screen pixels → WPF DIPs
        _editPosX = ((newL - _mapLeft) / _mapScale + _mapOffsetX) / _dpiScaleX;
        _editPosY = ((newT - _mapTop)  / _mapScale + _mapOffsetY) / _dpiScaleY;

        OnPropertyChanged(nameof(WidgetPositionText));

        if (_livePreviewTarget != null)
        {
            _livePreviewTarget.Left = _editPosX;
            _livePreviewTarget.Top  = _editPosY;
        }

        e.Handled = true;
    }

    private void WidgetMarker_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_markerDragging) return;
        _markerDragging = false;
        _widgetMarker?.ReleaseMouseCapture();
        e.Handled = true;
    }

    // ── Save / Cancel ────────────────────────────────────────────────────────

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _widget.Name             = WidgetName.Trim().Length > 0 ? WidgetName.Trim() : _widget.Name;
        _widget.Places           = [.. Places];
        _widget.FontSize         = FontSizeValue;
        _widget.DateTimeFormat   = _globalFormat.Trim().Length > 0 ? _globalFormat.Trim() : _widget.DateTimeFormat;
        _widget.ShowTitleBar     = ShowTitleBar;
        _widget.BackgroundColor  = _bgColorHex;
        _widget.BackgroundOpacity = _bgOpacityPercent / 100.0;

        MonitorService.SavePosition(_widget, _editPosX, _editPosY);
        if (_livePreviewTarget != null)
        {
            _livePreviewTarget.Left = _editPosX;
            _livePreviewTarget.Top  = _editPosY;
        }

        AutoStartService.SetEnabled(AutoStartEnabled);
        App.Settings.AutoStart = AutoStartEnabled;

        SettingsService.Save(App.Settings);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _livePreviewTarget?.ApplyFontSize(_origFontSize);
        _livePreviewTarget?.ApplyBackground(_origBgColor, _origBgOpacityPercent / 100.0);
        if (_livePreviewTarget != null)
        {
            _livePreviewTarget.Left = _origPosX;
            _livePreviewTarget.Top  = _origPosY;
        }
        DialogResult = false;
        Close();
    }

    // ── INotifyPropertyChanged ───────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
