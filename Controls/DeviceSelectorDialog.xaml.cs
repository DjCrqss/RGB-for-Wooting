using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wooting;

namespace WootingRGB.Controls;

public partial class DeviceSelectorDialog : Window
{
    public int? SelectedDeviceIndex { get; private set; }

    public DeviceSelectorDialog(RGBDeviceInfo[] devices)
    {
        InitializeComponent();
        PopulateDevices(devices);
    }

    private void PopulateDevices(RGBDeviceInfo[] devices)
    {
        for (int i = 0; i < devices.Length; i++)
        {
            var device = devices[i];
            int deviceIndex = i;

            var button = new Button
            {
                Style = (Style)FindResource("DeviceButton")
            };

            // Create content panel
            var panel = new StackPanel();

            // Device name
            var nameText = new TextBlock
            {
                Text = $"Device {i + 1}: {device.Model}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                Margin = new Thickness(0, 0, 0, 5)
            };
            panel.Children.Add(nameText);

            // Device details
            var detailsText = new TextBlock
            {
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999")),
                FontSize = 12
            };
            detailsText.Inlines.Add($"Type: {device.DeviceType}  •  ");
            detailsText.Inlines.Add($"Layout: {device.MaxRows}×{device.MaxColumns}  •  ");
            detailsText.Inlines.Add($"Status: {(device.Connected ? "Connected" : "Disconnected")}");
            panel.Children.Add(detailsText);

            button.Content = panel;
            button.Click += (s, e) =>
            {
                SelectedDeviceIndex = deviceIndex;
                DialogResult = true;
                Close();
            };

            DevicesPanel.Children.Add(button);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
