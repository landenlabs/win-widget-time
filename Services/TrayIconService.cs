// Copyright (c) 2026 LanDen Labs - Dennis Lang
using Bitmap = System.Drawing.Bitmap;
using Icon   = System.Drawing.Icon;
using Pen    = System.Drawing.Pen;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WinWidgetTime.Models;

namespace WinWidgetTime.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;

    // Static separators/items rebuilt on Opening
    private readonly ToolStripMenuItem _addWidgetItem;
    private readonly ToolStripSeparator _topSep  = new();
    private readonly ToolStripSeparator _botSep  = new();
    private readonly ToolStripMenuItem _aboutItem;
    private readonly ToolStripMenuItem _exitItem;

    private readonly Func<IReadOnlyList<WidgetSettings>> _getWidgets;
    private readonly Action<string> _onWidgetSettings;
    private readonly Action<string> _onWidgetRemove;

    public TrayIconService(
        Action onAddWidget,
        Func<IReadOnlyList<WidgetSettings>> getWidgets,
        Action<string> onWidgetSettings,
        Action<string> onWidgetRemove,
        Action onAbout,
        Action onExit)
    {
        _getWidgets       = getWidgets;
        _onWidgetSettings = onWidgetSettings;
        _onWidgetRemove   = onWidgetRemove;

        _addWidgetItem = new ToolStripMenuItem("+ Add Widget", null, (_, _) => UIInvoke(onAddWidget));
        _aboutItem     = new ToolStripMenuItem("About",        null, (_, _) => UIInvoke(onAbout));
        _exitItem      = new ToolStripMenuItem("Exit",         null, (_, _) => UIInvoke(onExit));

        _menu = new ContextMenuStrip();
        _menu.Opening += OnMenuOpening;

        _notifyIcon = new NotifyIcon
        {
            Text             = "WinWidgetTime",
            Icon             = BuildIcon(),
            ContextMenuStrip = _menu,
            Visible          = true
        };
        _notifyIcon.DoubleClick += (_, _) => UIInvoke(() => onWidgetSettings(getWidgets().FirstOrDefault()?.Id ?? ""));
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }

    private void OnMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _menu.Items.Clear();
        _menu.Items.Add(_addWidgetItem);
        _menu.Items.Add(_topSep);

        var widgets = _getWidgets();
        foreach (var widget in widgets)
        {
            var id = widget.Id;
            var name = widget.Name;
            var widgetMenu = new ToolStripMenuItem(name);
            widgetMenu.DropDownItems.Add("Settings", null, (_, _) => UIInvoke(() => _onWidgetSettings(id)));
            widgetMenu.DropDownItems.Add("Remove",   null, (_, _) => UIInvoke(() => _onWidgetRemove(id)));

            // Disable Remove when it's the only widget
            ((ToolStripMenuItem)widgetMenu.DropDownItems[1]).Enabled = widgets.Count > 1;

            _menu.Items.Add(widgetMenu);
        }

        _menu.Items.Add(_botSep);
        _menu.Items.Add(_aboutItem);
        _menu.Items.Add(_exitItem);
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
