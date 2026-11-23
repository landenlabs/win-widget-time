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
    /// <summary>Font size range for the time rows (matches the Settings slider).</summary>
    public const int MinFontSize = 10;
    public const int MaxFontSize = 36;

    // Smallest size a corner drag may shrink the widget to before the font clamp takes over (DIPs).
    private const double MinResizeWidth  = 60;
    private const double MinResizeHeight = 30;

    private readonly WidgetSettings _widget;
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _displayCheckTimer;
    private List<TimeDisplayItem> _items = [];
    private bool _isEmbedded;

    // Widget top-left in SCREEN PHYSICAL PIXELS — the single source of truth for position,
    // whether embedded (child of WorkerW) or a normal top-level window. See WidgetGeometry.
    private int _posX;
    private int _posY;
    private string _currentFingerprint;

    // Drag (move) and resize share the same start snapshot.
    private bool _isDragging;
    private bool _isResizing;
    private ResizeZone _resizeZone;
    private DesktopService.POINT _startCursor;
    private int _startX, _startY;
    private double _startW, _startH;          // DIPs
    private int _startPixelW, _startPixelH;
    private int _startFontSize;

    private SettingsWindow? _settingsDialog;

    public string WidgetId => _widget.Id;

    /// <summary>Current top-left in screen physical pixels.</summary>
    public (int X, int Y) ScreenPosition => (_posX, _posY);

    public event Action<WidgetWindow>? RemoveRequested;

    private double DpiX => VisualTreeHelper.GetDpi(this).DpiScaleX;
    private double DpiY => VisualTreeHelper.GetDpi(this).DpiScaleY;

    // The window sizes to its content, so measure the border (Width/Height are NaN).
    private int PixelWidth  => (int)Math.Round(WidgetBorder.ActualWidth  * DpiX);
    private int PixelHeight => (int)Math.Round(WidgetBorder.ActualHeight * DpiY);
    private PxRect PixelBounds => new(_posX, _posY, PixelWidth, PixelHeight);

    private int CurrentFontSize => (int)Math.Round(TimeList.FontSize);

    public WidgetWindow(WidgetSettings widget)
    {
        _widget = widget;
        InitializeComponent();

        _currentFingerprint = MonitorService.GetFingerprint();
        (_posX, _posY) = MonitorService.GetPosition(_widget);
        // Provisional (assumes 100 %); corrected for the real DPI in OnSourceInitialized before first show.
        Left = _posX;
        Top  = _posY;

        LoadItems();

        ContextMenuOpening += (_, _) => RemoveMenuItem.IsEnabled = App.Settings.Widgets.Count > 1;
        SizeChanged += (_, _) =>
        {
            // Child windows (embedded in WorkerW) need an explicit MoveWindow to pick up a new size.
            if (_isEmbedded && !_isResizing)
                MoveTo(_posX, _posY);
        };
        Loaded += (_, _) =>
        {
            // First point where the content size is known: re-place and recover if off-screen.
            MoveTo(_posX, _posY);
            EnsureOnScreen();
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();

        // Pull the widget back if its monitor is unplugged (or the layout otherwise changes) while running.
        _displayCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _displayCheckTimer.Tick += (_, _) => CheckDisplayConfigurationChanged();
        _displayCheckTimer.Start();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        ApplyFontSize(_widget.FontSize);
        ApplyBackground(_widget.BackgroundColor, _widget.BackgroundOpacity);
        ApplyShowTitleBar(_widget.ShowTitleBar);

        DesktopService.HideFromAltTab(this);
        _isEmbedded = _widget.EmbedInWallpaper && DesktopService.EmbedInWallpaper(this);
        if (!_isEmbedded)
            DesktopService.SetAlwaysOnBottom(this);

        MoveTo(_posX, _posY);
        EnsureOnScreen();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        _displayCheckTimer.Stop();
        base.OnClosed(e);
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

    public void ApplyFontSize(int size) => TimeList.FontSize = Math.Clamp(size, MinFontSize, MaxFontSize);

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
            item.Update(_widget.DateTimeFormat);
    }

    // ── Position (screen pixels) ─────────────────────────────────────────────

    /// <summary>Moves the widget so its top-left is at the given screen pixel position (does not save).</summary>
    public void MoveTo(int x, int y)
    {
        _posX = x;
        _posY = y;
        if (_isEmbedded)
        {
            // Before the first layout the content size is unknown; keep the current HWND size then.
            bool measured = WidgetBorder.ActualWidth > 0 && WidgetBorder.ActualHeight > 0;
            DesktopService.MoveEmbeddedWindow(this, x, y,
                measured ? PixelWidth : null, measured ? PixelHeight : null);
        }
        else
        {
            Left = x / DpiX;
            Top  = y / DpiY;
        }
    }

    /// <summary>Persists the current position for the current monitor layout.</summary>
    public void SavePosition()
    {
        MonitorService.SavePosition(_widget, _posX, _posY);
        SettingsService.Save(App.Settings);
    }

    /// <summary>Pulls the widget back onto a screen if it's off every monitor (e.g. after unplugging one).</summary>
    private void EnsureOnScreen()
    {
        var fix = WidgetGeometry.EnsureVisible(PixelBounds, MonitorService.GetScreens());
        if (fix is not { } p) return;
        MoveTo(p.X, p.Y);
        SavePosition();
    }

    private void CheckDisplayConfigurationChanged()
    {
        if (_isDragging || _isResizing) return;

        var fingerprint = MonitorService.GetFingerprint();
        if (fingerprint == _currentFingerprint) return;

        _currentFingerprint = fingerprint;
        var (x, y) = MonitorService.GetPosition(_widget);
        MoveTo(x, y);
        EnsureOnScreen();
    }

    // ── Controls ────────────────────────────────────────────────────────────

    public void OpenSettings()
    {
        // Tray double-click / tray menu / widget menu can all request this while it's already open.
        if (_settingsDialog != null)
        {
            _settingsDialog.Activate();
            return;
        }

        bool ok;
        try
        {
            _settingsDialog = new SettingsWindow(_widget, livePreviewTarget: this);
            ok = _settingsDialog.ShowDialog() == true;
        }
        finally
        {
            _settingsDialog = null;
        }

        // On Cancel / Esc / X the SettingsWindow has already reverted its live preview.
        if (ok)
            LoadItems();
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void About_Click(object sender, RoutedEventArgs e)
        => ((App)Application.Current).OpenAbout(this);

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (App.Settings.Widgets.Count <= 1)
        {
            MessageBox.Show(
                "This is the only widget. Use Exit to close the app instead.",
                "Remove Widget", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            "Remove this widget?",
            "Confirm Remove",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
            RemoveRequested?.Invoke(this);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
        => Application.Current.Shutdown();

    // ── Hover: show/hide icon buttons ───────────────────────────────────────

    private void Widget_MouseEnter(object sender, MouseEventArgs e)
        => IconPanel.Visibility = Visibility.Visible;

    private void Widget_MouseLeave(object sender, MouseEventArgs e)
    {
        IconPanel.Visibility = Visibility.Collapsed;
        if (!_isDragging && !_isResizing)
            UpdateHoverCursor(ResizeZone.None);
    }

    // ── Dragging (move) ─────────────────────────────────────────────────────

    private void Widget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsClickOnInteractiveElement(e.OriginalSource))
            return;

        var zone = GetResizeZone(e.GetPosition(WidgetBorder));
        if (zone != ResizeZone.None)
        {
            StartResize(zone);
            e.Handled = true;
            return;
        }

        // Manual drag (DragMove() fails on Windows 10 with AllowsTransparency=True + WindowStyle=None,
        // and doesn't work at all for a window embedded in WorkerW). Everything is in screen pixels.
        SnapshotStart();
        _isDragging = true;
        SetCapturedCursor(Cursors.SizeAll);
        WidgetBorder.CaptureMouse();
        e.Handled = true;
    }

    private void SnapshotStart()
    {
        _startCursor   = DesktopService.GetCursorPosition();
        _startX        = _posX;
        _startY        = _posY;
        _startW        = WidgetBorder.ActualWidth;
        _startH        = WidgetBorder.ActualHeight;
        _startPixelW   = PixelWidth;
        _startPixelH   = PixelHeight;
        _startFontSize = CurrentFontSize;
    }

    private void Widget_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var c = DesktopService.GetCursorPosition();
            var (x, y) = WidgetGeometry.DragTo(_startX, _startY, _startCursor.X, _startCursor.Y, c.X, c.Y);
            MoveTo(x, y);
            return;
        }

        if (_isResizing)
        {
            UpdateResize();
            return;
        }

        UpdateHoverCursor(GetResizeZone(e.GetPosition(WidgetBorder)));
    }

    /// <summary>The body shows the move cursor; the resize corners force their diagonal cursor.</summary>
    private void UpdateHoverCursor(ResizeZone zone)
    {
        bool inCorner = zone != ResizeZone.None;
        WidgetBorder.Cursor = inCorner ? CursorFor(zone) : Cursors.SizeAll;
        WidgetBorder.ForceCursor = inCorner;
    }

    /// <summary>
    /// While the border has mouse capture (drag/resize), it's the element queried for the
    /// cursor, so pin the appropriate cursor for the duration of the operation.
    /// </summary>
    private void SetCapturedCursor(System.Windows.Input.Cursor cursor)
    {
        WidgetBorder.Cursor = cursor;
        WidgetBorder.ForceCursor = true;
    }

    private void Widget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging && !_isResizing) return;
        EndDragOrResize();
        UpdateHoverCursor(GetResizeZone(e.GetPosition(WidgetBorder)));
        e.Handled = true;
    }

    /// <summary>Capture can be lost mid-drag (Alt-Tab, Win key, UAC prompt); finish cleanly instead of sticking.</summary>
    private void Widget_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_isDragging || _isResizing)
            EndDragOrResize();
    }

    private void EndDragOrResize()
    {
        bool wasResizing = _isResizing;
        _isDragging = false;
        _isResizing = false;
        _resizeZone = ResizeZone.None;
        if (WidgetBorder.IsMouseCaptured)
            WidgetBorder.ReleaseMouseCapture();
        UpdateHoverCursor(ResizeZone.None);

        if (wasResizing)
            _widget.FontSize = CurrentFontSize;
        EnsureOnScreen();
        SavePosition();
    }

    private bool IsClickOnInteractiveElement(object? source)
    {
        if (source is System.Windows.Controls.Button) return true;
        if (source is FrameworkElement fe)
        {
            var parent = VisualTreeHelper.GetParent(fe);
            while (parent != null)
            {
                if (parent == IconPanel) return true;
                parent = VisualTreeHelper.GetParent(parent);
            }
        }
        return false;
    }

    // ── Resizing (scales the font; the window sizes to its content) ──────────

    /// <summary>
    /// Bottom corners always resize. Top corners only resize when the title bar is
    /// hidden — while it's shown, the top of the widget is the drag/move zone instead.
    /// </summary>
    private ResizeZone GetResizeZone(Point pos) =>
        WidgetGeometry.HitTest(pos.X, pos.Y, WidgetBorder.ActualWidth, WidgetBorder.ActualHeight,
                               allowTopCorners: !_widget.ShowTitleBar);

    private static System.Windows.Input.Cursor CursorFor(ResizeZone zone) => zone switch
    {
        ResizeZone.TopLeft or ResizeZone.BottomRight => Cursors.SizeNWSE,
        ResizeZone.TopRight or ResizeZone.BottomLeft => Cursors.SizeNESW,
        _ => Cursors.SizeAll
    };

    private void StartResize(ResizeZone zone)
    {
        SnapshotStart();
        _isResizing = true;
        _resizeZone = zone;
        SetCapturedCursor(CursorFor(zone));
        WidgetBorder.CaptureMouse();
    }

    /// <summary>
    /// A corner drag computes the size the widget "would" have, then scales the font by the same
    /// ratio (see <see cref="WidgetGeometry.ScalePercentFromResize"/>); the window re-sizes to the
    /// new content. The edges opposite the dragged corner stay fixed.
    /// </summary>
    private void UpdateResize()
    {
        var c = DesktopService.GetCursorPosition();
        var r = WidgetGeometry.Resize(_resizeZone, _startW, _startH, _startX, _startY,
                                      c.X - _startCursor.X, c.Y - _startCursor.Y,
                                      DpiX, DpiY, MinResizeWidth, MinResizeHeight);

        int size = WidgetGeometry.ScalePercentFromResize(_startFontSize, _startW, _startH, r.Width, r.Height,
                                                         MinFontSize, MaxFontSize);
        if (size == CurrentFontSize) return;

        ApplyFontSize(size);
        UpdateLayout();

        int x = WidgetGeometry.IsWest(_resizeZone)  ? _startX + (_startPixelW - PixelWidth)  : _startX;
        int y = WidgetGeometry.IsNorth(_resizeZone) ? _startY + (_startPixelH - PixelHeight) : _startY;
        MoveTo(x, y);
    }
}
