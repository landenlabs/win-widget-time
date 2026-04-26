// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace WinWidgetTime.Windows;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        Topmost = true;

        var version = Assembly.GetExecutingAssembly()
                               .GetName().Version?.ToString() ?? "?";
        VersionText.Text = $"v{version}  ·  World clock desktop widget";

        var mp4 = Path.Combine(AppContext.BaseDirectory, "Assets", "landen_labs.mp4");
        if (File.Exists(mp4))
        {
            LogoPlayer.Source = new Uri(mp4);
            LogoPlayer.Visibility = System.Windows.Visibility.Visible;
        }
        else
        {
            var png = Path.Combine(AppContext.BaseDirectory, "Assets", "landenlabs.png");
            if (File.Exists(png))
            {
                LogoFallback.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(png));
                LogoFallback.Visibility = System.Windows.Visibility.Visible;
            }
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
