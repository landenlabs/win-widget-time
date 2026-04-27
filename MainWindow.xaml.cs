// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WinWidgetTime.Services;
using WinWidgetTime.ViewModels;
using WinWidgetTime.Windows;

namespace WinWidgetTime;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;
    private List<TimeDisplayItem> _items = [];
    private bool _isEmbedded;

    // Dragging state
    private bool _isDragging;
    private System.Windows.Point _dragOffset; // widget-relative offset at drag start (embedded: pixels)

    public MainWindow()
    {
        InitializeComponent();

        var (x, y) = MonitorService.GetPosition(App.Settings);
        (x, y) = MonitorService.ClampToScreen(x, y);

        // Keep settings in sync so OnSourceInitialized picks up the validated position
        App.Settings.PositionX = x;
        App.Settings.PositionY = y;

        Left = x;
        Top  = y;

        LoadItems();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        ApplyFontSize(App.Settings.FontSize);
        ApplyBackground(App.Settings.BackgroundColor, App.Settings.BackgroundOpacity);

        if (App.Settings.EmbedInWallpaper)
        {
            _isEmbedded = DesktopService.EmbedInWallpaper(this);
            if (_isEmbedded)
                DesktopService.MoveEmbeddedWindow(this, (int)App.Settings.PositionX, (int)App.Settings.PositionY);
            else
                DesktopService.SetAlwaysOnBottom(this);
        }
        else
        {
            DesktopService.SetAlwaysOnBottom(this);
        }
    }

    public void LoadItems()
    {
        _items = App.Settings.Places.Select(p => new TimeDisplayItem(p)).ToList();
        TimeList.ItemsSource = _items;
        ApplyFontSize(App.Settings.FontSize);
        ApplyBackground(App.Settings.BackgroundColor, App.Settings.BackgroundOpacity);
    }

    public void ApplyFontSize(int size) => TimeList.FontSize = size;

    public void ApplyBackground(string hexColor, double opacity)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            WidgetBorder.Background = new SolidColorBrush(color) { Opacity = opacity };
        }
        catch { }
    }

    private void Tick()
    {
        foreach (var item in _items)
            item.Update(App.Settings.DateFormat, App.Settings.TimeFormat);
    }

    // ── Controls ────────────────────────────────────────────────────────────

    public void OpenSettings()
    {
        new SettingsWindow().ShowDialog();
        LoadItems();
    }

    public void OpenAbout() => new AboutWindow().ShowDialog();

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();
    private void About_Click(object sender, RoutedEventArgs e)    => OpenAbout();

    private void Exit_Click(object sender, RoutedEventArgs e)
        => Application.Current.Shutdown();

    // ── Hover: show/hide icon buttons ───────────────────────────────────────

    private void Widget_MouseEnter(object sender, MouseEventArgs e)
        => IconPanel.Visibility = Visibility.Visible;

    private void Widget_MouseLeave(object sender, MouseEventArgs e)
        => IconPanel.Visibility = Visibility.Collapsed;

    // ── Dragging ────────────────────────────────────────────────────────────

    private void Widget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isEmbedded)
        {
            var cursor = DesktopService.GetCursorPosition();
            var bounds = DesktopService.GetWindowBounds(this);
            _dragOffset = new System.Windows.Point(cursor.X - bounds.Left, cursor.Y - bounds.Top);
            _isDragging = true;
            WidgetBorder.CaptureMouse();
        }
        else
        {
            // DragMove handles everything for top-level windows
            DragMove();
            SavePosition();
        }
    }

    private void Widget_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || !_isEmbedded) return;

        var cursor = DesktopService.GetCursorPosition();
        int newX = cursor.X - (int)_dragOffset.X;
        int newY = cursor.Y - (int)_dragOffset.Y;
        DesktopService.MoveEmbeddedWindow(this, newX, newY);
    }

    private void Widget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        WidgetBorder.ReleaseMouseCapture();

        if (_isEmbedded)
        {
            var bounds = DesktopService.GetWindowBounds(this);
            MonitorService.SavePosition(App.Settings, bounds.Left, bounds.Top);
            SettingsService.Save(App.Settings);
        }
    }

    private void SavePosition()
    {
        MonitorService.SavePosition(App.Settings, Left, Top);
        SettingsService.Save(App.Settings);
    }
}
