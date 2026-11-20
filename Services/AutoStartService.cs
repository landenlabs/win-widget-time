// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Windows.ApplicationModel;

namespace WinWidgetTime.Services;

/// <summary>
/// Auto-start on login. Portable (unpackaged) builds use the HKCU Run key. MSIX builds
/// can't — their registry writes are virtualized — so they use the package's
/// windows.startupTask extension (TaskId <see cref="StartupTaskId"/>, declared in
/// the MSIX manifest by .github/scripts/build-msix.ps1).
/// </summary>
public static class AutoStartService
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WinWidgetTime";
    public const string StartupTaskId = "WinWidgetTimeStartup";

    /// <summary>True when running from an installed MSIX package.</summary>
    public static bool IsPackaged { get; } = DetectPackaged();

    public static bool IsEnabled()
    {
        if (IsPackaged)
        {
            var state = GetStartupTask()?.State;
            return state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
        }

        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppName) != null;
    }

    /// <summary>
    /// Enables or disables auto-start. Returns null on success, otherwise a message
    /// explaining why the change couldn't be applied (e.g. the user disabled the
    /// startup entry in Task Manager, which apps aren't allowed to override).
    /// </summary>
    public static string? SetEnabled(bool enable)
    {
        if (IsPackaged)
            return SetPackagedEnabled(enable);

        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return null;

        if (enable)
        {
            var exePath = Environment.ProcessPath
                ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            key.SetValue(AppName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
        return null;
    }

    private static string? SetPackagedEnabled(bool enable)
    {
        var task = GetStartupTask();
        if (task == null) return "The startup task could not be found in the app package.";

        if (!enable)
        {
            if (task.State == StartupTaskState.Enabled) task.Disable();
            return null;
        }

        var state = task.State == StartupTaskState.Disabled
            ? Run(() => task.RequestEnableAsync().AsTask())
            : task.State;

        return state switch
        {
            StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy => null,
            StartupTaskState.DisabledByUser =>
                "Auto-start was turned off in Task Manager › Startup apps.\n" +
                "Re-enable WinWidgetTime there to start it on login.",
            StartupTaskState.DisabledByPolicy =>
                "Auto-start is disabled by group policy on this PC.",
            _ => "Auto-start could not be enabled.",
        };
    }

    private static StartupTask? GetStartupTask()
    {
        try { return Run(() => StartupTask.GetAsync(StartupTaskId).AsTask()); }
        catch { return null; }
    }

    /// <summary>Blocks on a WinRT async op off the UI thread's sync context.</summary>
    private static T Run<T>(Func<Task<T>> op) => Task.Run(op).GetAwaiter().GetResult();

    private static bool DetectPackaged()
    {
        try
        {
            uint len = 0;
            const int APPMODEL_ERROR_NO_PACKAGE = 15700;
            return GetCurrentPackageFullName(ref len, null) != APPMODEL_ERROR_NO_PACKAGE;
        }
        catch { return false; }   // API missing (pre-Windows 8)
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, StringBuilder? packageFullName);
}
