// Copyright (c) 2026 LanDen Labs - Dennis Lang
using Bitmap = System.Drawing.Bitmap;
using Icon   = System.Drawing.Icon;
using Pen    = System.Drawing.Pen;
using System.Drawing.Drawing2D;
using System.IO;
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

    // Colored clock icon drawn at the system small-icon size (DPI aware) so it
    // stays crisp and visible on both light and dark taskbars.
    private static Icon BuildIcon()
    {
        int s = Math.Max(16, SystemInformation.SmallIconSize.Width);
        try
        {
            using var bmp = new Bitmap(s, s, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);

                float u   = s / 16f;                 // 1 "pixel" at 16px design size
                float pad = 0.5f * u;
                var face  = new System.Drawing.RectangleF(pad, pad, s - 2 * pad, s - 2 * pad);

                // Blue gradient face
                using (var faceBrush = new LinearGradientBrush(face,
                           System.Drawing.Color.FromArgb(80, 170, 255),
                           System.Drawing.Color.FromArgb(20, 80, 190), 90f))
                    g.FillEllipse(faceBrush, face);

                // White rim
                using (var rimPen = new Pen(System.Drawing.Color.White, 1.2f * u))
                    g.DrawEllipse(rimPen, face.X + 0.6f * u, face.Y + 0.6f * u,
                                  face.Width - 1.2f * u, face.Height - 1.2f * u);

                float cx = s / 2f, cy = s / 2f;

                // Hour ticks at 12 / 3 / 6 / 9
                using (var tickPen = new Pen(System.Drawing.Color.White, 1.2f * u))
                {
                    float r1 = 5.2f * u, r2 = 6.4f * u;
                    for (int i = 0; i < 4; i++)
                    {
                        double a = i * Math.PI / 2;
                        g.DrawLine(tickPen,
                            cx + (float)Math.Sin(a) * r1, cy - (float)Math.Cos(a) * r1,
                            cx + (float)Math.Sin(a) * r2, cy - (float)Math.Cos(a) * r2);
                    }
                }

                // Hour hand (white, ~10 o'clock) and minute hand (amber, ~2 o'clock)
                using (var hourPen = new Pen(System.Drawing.Color.White, 1.6f * u)
                           { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLine(hourPen, cx, cy, cx - 3.0f * u, cy - 2.0f * u);
                using (var minPen = new Pen(System.Drawing.Color.FromArgb(255, 190, 40), 1.4f * u)
                           { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLine(minPen, cx, cy, cx + 2.8f * u, cy - 4.2f * u);

                // Red hub
                float hub = 1.4f * u;
                using (var hubBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(235, 60, 60)))
                    g.FillEllipse(hubBrush, cx - hub, cy - hub, 2 * hub, 2 * hub);
            }
            return Icon.FromHandle(bmp.GetHicon());
        }
        catch
        {
            return System.Drawing.SystemIcons.Application;
        }
    }
}
