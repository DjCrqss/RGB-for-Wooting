using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WootingRGB.Controls;

public partial class ColorPickerDialog : Window
{
    public Color SelectedColor { get; private set; }

    public ColorPickerDialog(Color initialColor)
    {
        InitializeComponent();
        SelectedColor = initialColor;
        
        RedSlider.Value = initialColor.R;
        GreenSlider.Value = initialColor.G;
        BlueSlider.Value = initialColor.B;
        
        UpdateColorPreview();
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateColorPreview();
    }

    private void UpdateColorPreview()
    {
        if (ColorPreview == null) return;

        var r = (byte)RedSlider.Value;
        var g = (byte)GreenSlider.Value;
        var b = (byte)BlueSlider.Value;

        SelectedColor = Color.FromRgb(r, g, b);
        ColorPreview.Fill = new SolidColorBrush(SelectedColor);

        RedValueText.Text = r.ToString();
        GreenValueText.Text = g.ToString();
        BlueValueText.Text = b.ToString();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
