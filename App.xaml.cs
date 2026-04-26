using System.Threading;
using System.Windows;
using WinWidgetTime.Models;
using WinWidgetTime.Services;

namespace WinWidgetTime;

public partial class App : Application
{
    private static Mutex? _mutex;
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
        new MainWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
