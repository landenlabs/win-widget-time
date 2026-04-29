// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WinWidgetTime.Models;
using WinWidgetTime.Services;
using WinWidgetTime.ViewModels;

namespace WinWidgetTime.Windows;

public partial class WidgetWindow : Window
{
    private readonly WidgetSettings _widget;
    private readonly DispatcherTimer _timer;
    private List<TimeDisplayItem> _items = [];
    private bool _isEmbedded;

    // Dragging state
    private bool _isDragging;
    private System.Windows.Point _dragOffset;

    public string WidgetId => _widget.Id;

    public event Action<WidgetWindow>? RemoveRequested;

    public WidgetWindow(WidgetSettings widget)
    {
        _widget = widget;
        InitializeComponent();

        var (x, y) = MonitorService.GetPosition(_widget);
        (x, y) = MonitorService.ClampToScreen(x, y);

        _widget.PositionX = x;
        _widget.PositionY = y;

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

        ApplyFontSize(_widget.FontSize);
        ApplyBackground(_widget.BackgroundColor, _widget.BackgroundOpacity);
        ApplyShowTitleBar(_widget.ShowTitleBar);

        if (_widget.EmbedInWallpaper)
        {
            _isEmbedded = DesktopService.EmbedInWallpaper(this);
            if (_isEmbedded)
                DesktopService.MoveEmbeddedWindow(this, (int)_widget.PositionX, (int)_widget.PositionY);
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
        _items = _widget.Places.Select(p => new TimeDisplayItem(p)).ToList();
        TimeList.ItemsSource = _items;
        TitleText.Text = $"🕐 {_widget.Name}";
        ApplyFontSize(_widget.FontSize);
        ApplyBackground(_widget.BackgroundColor, _widget.BackgroundOpacity);
        ApplyShowTitleBar(_widget.ShowTitleBar);

        // Disable "Remove Widget" when it would eliminate the last widget
        if (RemoveMenuItem != null)
            RemoveMenuItem.IsEnabled = App.Settings.Widgets.Count > 1;
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

    public void ApplyShowTitleBar(bool show)
    {
        TitleBarGrid.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        TitleBarSeparator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Tick()
    {
        foreach (var item in _items)
            item.Update(_widget.DateFormat, _widget.TimeFormat);
    }

    // ── Controls ────────────────────────────────────────────────────────────

    public void OpenSettings()
    {
        var dlg = new SettingsWindow(_widget, livePreviewTarget: this);
        if (dlg.ShowDialog() == true)
            LoadItems();
        else
        {
            // Revert any live-preview changes that were not saved
            ApplyFontSize(_widget.FontSize);
            ApplyBackground(_widget.BackgroundColor, _widget.BackgroundOpacity);
        }
    }

    public void OpenAbout() => new AboutWindow().ShowDialog();

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();
    private void About_Click(object sender, RoutedEventArgs e)    => OpenAbout();

    private void Remove_Click(object sender, RoutedEventArgs e)
        => RemoveRequested?.Invoke(this);

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
            MonitorService.SavePosition(_widget, bounds.Left, bounds.Top);
            SettingsService.Save(App.Settings);
        }
    }

    private void SavePosition()
    {
        MonitorService.SavePosition(_widget, Left, Top);
        SettingsService.Save(App.Settings);
    }
}
