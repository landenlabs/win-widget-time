using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WinWidgetTime.Windows;

public partial class ColorPickerWindow : Window
{
    private static readonly string[] Swatches =
    [
        "#FFFFFF", "#C0C0C0", "#808080", "#404040", "#202020", "#000000",
        "#FF4444", "#FF6B35", "#FF9500", "#FFD700", "#FFFF44", "#AAFF00",
        "#00FF44", "#00FF88", "#00FFCC", "#00DDFF", "#0099FF", "#4466FF",
        "#8844FF", "#CC00FF", "#FF00CC", "#FF0077", "#FF6699", "#FFCCDD",
        "#FF00FF", "#00FFFF", "#88FF88", "#FFBB66", "#AACCFF", "#FFAAAA",
        "#E63946", "#2EC4B6", "#FF9F1C", "#CBFF8C", "#011627", "#FDFFFC",
    ];

    public Color SelectedColor { get; private set; } = Colors.White;
    private bool _suppressHexEvent;

    public ColorPickerWindow(string initialHex = "#FFFFFF")
    {
        InitializeComponent();

        BuildSwatches();

        try { SelectedColor = (Color)ColorConverter.ConvertFromString(initialHex); }
        catch { SelectedColor = Colors.White; }

        _suppressHexEvent = true;
        HexBox.Text = initialHex.ToUpperInvariant();
        _suppressHexEvent = false;
        UpdatePreview(SelectedColor);
    }

    private void BuildSwatches()
    {
        foreach (var hex in Swatches)
        {
            Color c;
            try { c = (Color)ColorConverter.ConvertFromString(hex); }
            catch { c = Colors.White; }

            var border = new Border
            {
                Background = new SolidColorBrush(c),
                Margin = new Thickness(2),
                CornerRadius = new CornerRadius(3),
                Cursor = Cursors.Hand,
                ToolTip = hex
            };
            border.MouseLeftButtonDown += (_, _) => SwatchClicked(hex, c);
            SwatchGrid.Children.Add(border);
        }
    }

    private void SwatchClicked(string hex, Color c)
    {
        SelectedColor = c;
        _suppressHexEvent = true;
        HexBox.Text = hex.ToUpperInvariant();
        _suppressHexEvent = false;
        UpdatePreview(c);
    }

    private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressHexEvent) return;
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(HexBox.Text);
            SelectedColor = c;
            UpdatePreview(c);
        }
        catch { /* ignore invalid input */ }
    }

    private void UpdatePreview(Color c)
        => PreviewSwatch.Background = new SolidColorBrush(c);

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
