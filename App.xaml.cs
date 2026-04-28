// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.Threading;
using System.Windows;
using WinWidgetTime.Models;
using WinWidgetTime.Services;
using WinWidgetTime.Windows;

namespace WinWidgetTime;

public partial class App : Application
{
    private static Mutex? _mutex;
    private TrayIconService? _trayIcon;
    private readonly List<WidgetWindow> _widgetWindows = [];

    public static AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, "WinWidgetTime_UniqueInstance_v1", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("WinWidgetTime is already running.",
                "WinWidgetTime", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        Settings = SettingsService.Load();

        foreach (var widget in Settings.Widgets)
            CreateAndShowWidget(widget);

        _trayIcon = new TrayIconService(
            onAddWidget:      AddWidget,
            getWidgets:       () => Settings.Widgets,
            onWidgetSettings: id => _widgetWindows.FirstOrDefault(w => w.WidgetId == id)?.OpenSettings(),
            onWidgetRemove:   RemoveWidget,
            onAbout:          OpenAbout,
            onExit:           Shutdown
        );
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private void CreateAndShowWidget(WidgetSettings widget)
    {
        var win = new WidgetWindow(widget);
        win.RemoveRequested += w => RemoveWidget(w.WidgetId);
        _widgetWindows.Add(win);
        win.Show();
    }

    private void AddWidget()
    {
        var widget = SettingsService.DefaultWidget();
        widget.Name = $"Widget {Settings.Widgets.Count + 1}";

        // Offset each new widget so they don't stack exactly on top of existing ones
        widget.PositionX = 50 + Settings.Widgets.Count * 30;
        widget.PositionY = 200 + Settings.Widgets.Count * 30;

        // Pre-add so SettingsWindow.Save_Click includes it in the persisted file
        Settings.Widgets.Add(widget);

        var dlg = new SettingsWindow(widget, livePreviewTarget: null);
        if (dlg.ShowDialog() == true)
        {
            // SettingsWindow already saved to disk; now create and show the window
            CreateAndShowWidget(widget);
        }
        else
        {
            // User cancelled — remove the pre-added entry (SettingsWindow did not save)
            Settings.Widgets.Remove(widget);
        }
    }

    private void RemoveWidget(string widgetId)
    {
        if (Settings.Widgets.Count <= 1) return;

        var win    = _widgetWindows.FirstOrDefault(w => w.WidgetId == widgetId);
        var widget = Settings.Widgets.FirstOrDefault(w => w.Id == widgetId);

        if (win != null) { _widgetWindows.Remove(win); win.Close(); }
        if (widget != null)
        {
            Settings.Widgets.Remove(widget);
            SettingsService.Save(Settings);
        }
    }

    private void OpenAbout() => new AboutWindow().ShowDialog();
}
