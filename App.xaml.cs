// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.Threading;
using System.Windows;
using WinWidgetTime.Models;
using WinWidgetTime.Services;

namespace WinWidgetTime;

public partial class App : Application
{
    private static Mutex? _mutex;
    private TrayIconService? _trayIcon;
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
        var mainWindow = new MainWindow();

        _trayIcon = new TrayIconService(
            onSettings: mainWindow.OpenSettings,
            onAbout:    mainWindow.OpenAbout,
            onExit:     Shutdown
        );

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
