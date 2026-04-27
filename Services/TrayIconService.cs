// Copyright (c) 2026 LanDen Labs - Dennis Lang
using Bitmap = System.Drawing.Bitmap;
using Icon   = System.Drawing.Icon;
using Pen    = System.Drawing.Pen;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinWidgetTime.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public TrayIconService(Action onSettings, Action onAbout, Action onExit)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings", null, (_, _) => UIInvoke(onSettings));
        menu.Items.Add("About",    null, (_, _) => UIInvoke(onAbout));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit",     null, (_, _) => UIInvoke(onExit));

        _notifyIcon = new NotifyIcon
        {
            Text             = "WinWidgetTime",
            Icon             = BuildIcon(),
            ContextMenuStrip = menu,
            Visible          = true
        };
        _notifyIcon.DoubleClick += (_, _) => UIInvoke(onSettings);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    // Dispatch to WPF UI thread so callers don't have to think about it.
    private static void UIInvoke(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null) return;
        if (dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }

    private static Icon BuildIcon()
    {
        const int S = 16;
        using var bmp = new Bitmap(S, S);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);

        using var pen = new Pen(System.Drawing.Color.FromArgb(220, 180, 220, 255), 1.5f);

        // Clock face circle
        g.DrawEllipse(pen, 1, 1, S - 3, S - 3);

        int cx = S / 2, cy = S / 2;
        g.DrawLine(pen, cx, cy, cx - 3, cy - 3); // hour hand  (~10 o'clock)
        g.DrawLine(pen, cx, cy, cx, cy - 5);      // minute hand (~12 o'clock)

        // App-lifetime icon — one HICON GDI object, freed on process exit
        return Icon.FromHandle(bmp.GetHicon());
    }
}
