using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WootingRGB.Controls;

public partial class ColorPickerDialog : Window
{
    public Color SelectedColor { get; private set; }
    private static readonly List<Color> RecentColors = new();
    private const int MaxRecentColors = 12;
    
    private double _hue = 0;
    private double _saturation = 1;
    private double _value = 1;
    private bool _updating = false;
    private bool _isDraggingCanvas = false;

    public event EventHandler<Color>? ColorChanged;

    public ColorPickerDialog(Color initialColor)
    {
        InitializeComponent();
        
        SelectedColor = initialColor;
        
        // Convert RGB to HSV
        (_hue, _saturation, _value) = RgbToHsv(initialColor);
        
        _updating = true;
        HueSlider.Value = _hue;
        _updating = false;
        
        UpdateHueGradient();
        UpdatePickerPosition();
        UpdateHexBox();
        LoadRecentColors();

        // Make window close when clicking outside
        this.Deactivated += (s, e) => 
        {
            if (this.IsActive == false)
                Close();
        };
    }

    private void ColorCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Grid grid)
        {
            _isDraggingCanvas = true;
            grid.CaptureMouse();
            UpdateFromCanvasPosition(e.GetPosition(grid));
        }
    }

    private void ColorCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _isDraggingCanvas && sender is Grid grid)
        {
            UpdateFromCanvasPosition(e.GetPosition(grid));
        }
    }

    private void ColorCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDraggingCanvas && sender is Grid grid)
        {
            _isDraggingCanvas = false;
            grid.ReleaseMouseCapture();
            
            // Notify color changed for live preview
            ColorChanged?.Invoke(this, SelectedColor);
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        
        if (_isDraggingCanvas)
        {
            _isDraggingCanvas = false;
            if (ColorCanvas.IsMouseCaptured)
            {
                ColorCanvas.ReleaseMouseCapture();
            }
            
            // Notify color changed for live preview
            ColorChanged?.Invoke(this, SelectedColor);
        }
    }

    private void UpdateFromCanvasPosition(Point position)
    {
        var canvas = ColorCanvas;
        _saturation = Math.Clamp(position.X / canvas.ActualWidth, 0, 1);
        _value = Math.Clamp(1 - (position.Y / canvas.ActualHeight), 0, 1);
        
        UpdatePickerPosition();
        UpdateColorFromHsv();
    }

    private void HueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating) return;
        
        _hue = e.NewValue;
        UpdateHueGradient();
        UpdateColorFromHsv();
        
        // Notify color changed for live preview
        ColorChanged?.Invoke(this, SelectedColor);
    }

    private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;
        
        var hex = HexTextBox.Text.Trim();
        if (hex.StartsWith("#")) hex = hex.Substring(1);
        
        if (hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int colorValue))
        {
            var r = (byte)((colorValue >> 16) & 0xFF);
            var g = (byte)((colorValue >> 8) & 0xFF);
            var b = (byte)(colorValue & 0xFF);
            
            SelectedColor = Color.FromRgb(r, g, b);
            (_hue, _saturation, _value) = RgbToHsv(SelectedColor);
            
            _updating = true;
            HueSlider.Value = _hue;
            _updating = false;
            
            UpdateHueGradient();
            UpdatePickerPosition();
            UpdateHexBox();
            
            // Notify color changed for live preview
            ColorChanged?.Invoke(this, SelectedColor);
        }
    }

    private void UpdateColorFromHsv()
    {
        SelectedColor = HsvToRgb(_hue, _saturation, _value);
        UpdateHexBox();
    }

    private void UpdateHueGradient()
    {
        var hueColor = HsvToRgb(_hue, 1, 1);
        HueColor.Color = hueColor;
    }

    private void UpdatePickerPosition()
    {
        var canvas = ColorCanvas;
        var x = _saturation * canvas.ActualWidth - 8;
        var y = (1 - _value) * canvas.ActualHeight - 8;
        
        PickerCircle.Margin = new Thickness(x, y, 0, 0);
    }

    private void UpdateHexBox()
    {
        _updating = true;
        HexTextBox.Text = $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
        _updating = false;
    }

    private void LoadRecentColors()
    {
        RecentColorsGrid.Children.Clear();
        
        foreach (var color in RecentColors)
        {
            var button = new Button
            {
                Background = new SolidColorBrush(color),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(2),
                Tag = color,
                Cursor = Cursors.Hand
            };
            
            button.Click += RecentColor_Click;
            RecentColorsGrid.Children.Add(button);
        }
        
        // Fill empty spots
        while (RecentColorsGrid.Children.Count < MaxRecentColors)
        {
            var emptyButton = new Button
            {
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(2),
                IsEnabled = false
            };
            RecentColorsGrid.Children.Add(emptyButton);
        }
    }

    private void RecentColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Color color)
        {
            SelectedColor = color;
            (_hue, _saturation, _value) = RgbToHsv(color);
            
            _updating = true;
            HueSlider.Value = _hue;
            _updating = false;
            
            UpdateHueGradient();
            UpdatePickerPosition();
            UpdateHexBox();
            
            // Notify color changed for live preview
            ColorChanged?.Invoke(this, SelectedColor);
        }
    }

    private static void AddToRecentColors(Color color)
    {
        RecentColors.Remove(color);
        RecentColors.Insert(0, color);
        
        if (RecentColors.Count > MaxRecentColors)
            RecentColors.RemoveAt(MaxRecentColors);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        AddToRecentColors(SelectedColor);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static (double h, double s, double v) RgbToHsv(Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double h = 0;
        if (delta != 0)
        {
            if (max == r)
                h = 60 * (((g - b) / delta) % 6);
            else if (max == g)
                h = 60 * (((b - r) / delta) + 2);
            else
                h = 60 * (((r - g) / delta) + 4);
        }
        if (h < 0) h += 360;

        double s = max == 0 ? 0 : delta / max;
        double v = max;

        return (h, s, v);
    }

    private static Color HsvToRgb(double h, double s, double v)
    {
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = v - c;

        double r = 0, g = 0, b = 0;

        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255)
        );
    }
}
