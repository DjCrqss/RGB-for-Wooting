using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WootingRGB.lib;

namespace WootingRGB.Controls;

public partial class ColorPickerDialog : UserControl
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
    public event EventHandler? PickerClosed;

    public ColorPickerDialog(Color initialColor)
    {
        InitializeComponent();
        
        SelectedColor = initialColor;
        
        // Convert RGB to HSV
        (_hue, _saturation, _value) = EffectUtilities.RgbToHsv(initialColor);
        
        _updating = true;
        HueSlider.Value = _hue;
        _updating = false;
        
        UpdateHueGradient();
        UpdateHexBox();
        LoadRecentColors();

        // Update picker position after the control is loaded and rendered
        this.Loaded += (s, e) =>
        {
            UpdatePickerPosition();
        };
    }

    public void OnClosing()
    {
        AddToRecentColors(SelectedColor);
        PickerClosed?.Invoke(this, EventArgs.Empty);
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
            
            // Notify color changed for live preview while dragging
            ColorChanged?.Invoke(this, SelectedColor);
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
            (_hue, _saturation, _value) = EffectUtilities.RgbToHsv(SelectedColor);
            
            _updating = true;
            HueSlider.Value = _hue;
            _updating = false;
            
            UpdateHueGradient();
            UpdatePickerPosition();
            
            // Notify color changed for live preview
            ColorChanged?.Invoke(this, SelectedColor);
        }
    }

    private void UpdateColorFromHsv()
    {
        SelectedColor = EffectUtilities.HsvToRgb(_hue, _saturation, _value);
        UpdateHexBox();
    }

    private void UpdateHueGradient()
    {
        var hueColor = EffectUtilities.HsvToRgb(_hue, 1, 1);
        HueColor.Color = hueColor;
    }

    private void UpdatePickerPosition()
    {
        var canvas = ColorCanvas;
        var x = _saturation * canvas.ActualWidth - 6;  // Half of 12px width
        var y = (1 - _value) * canvas.ActualHeight - 6; // Half of 12px height
        
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
                Tag = color,
                Style = (Style)FindResource("RecentColorButtonStyle")
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
                IsEnabled = false,
                Style = (Style)FindResource("RecentColorButtonStyle")
            };
            RecentColorsGrid.Children.Add(emptyButton);
        }
    }

    private void RecentColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Color color)
        {
            SelectedColor = color;
            (_hue, _saturation, _value) = EffectUtilities.RgbToHsv(color);
            
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
        // Remove if already exists
        RecentColors.Remove(color);
        
        // Add to front
        RecentColors.Insert(0, color);
        
        // Keep only max colors
        if (RecentColors.Count > MaxRecentColors)
            RecentColors.RemoveAt(MaxRecentColors);
    }
}
