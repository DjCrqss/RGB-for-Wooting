using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WootingRGB.Controls;
using WootingRGB.Core;
using WootingRGB.Managers;
using WootingRGB.Services;

namespace WootingRGB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IKeyboardService _keyboardService;
        private readonly AnalogInputService _analogInputService;
        private readonly EffectManager _effectManager;

        public MainWindow()
        {
            InitializeComponent();

            _keyboardService = new WootingKeyboardService();
            _analogInputService = new AnalogInputService();
            _effectManager = new EffectManager(_keyboardService, _analogInputService);

            _effectManager.EffectChanged += OnEffectChanged;

            InitializeEffectButtons();
        }

        private void InitializeEffectButtons()
        {
            foreach (var effect in _effectManager.AvailableEffects)
            {
                var button = new Button
                {
                    Content = effect.Name,
                    Width = 120,
                    Height = 40,
                    Margin = new Thickness(5),
                    Tag = effect
                };

                button.Click += EffectButton_Click;
                EffectButtonsPanel.Children.Add(button);
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_keyboardService.IsInitialized)
            {
                var kbSuccess = _keyboardService.Initialize();
                var analogSuccess = _analogInputService.Initialize();

                if (kbSuccess && analogSuccess)
                {
                    StatusText.Text = "Connected";
                    StatusText.Foreground = new SolidColorBrush(Colors.Green);
                    DeviceCountText.Text = $"{_keyboardService.DeviceCount} device(s) found";
                    ConnectButton.Content = "Disconnect";
                    EnableEffectButtons(true);
                }
                else
                {
                    StatusText.Text = "Connection Failed";
                    StatusText.Foreground = new SolidColorBrush(Colors.Red);
                    MessageBox.Show("Failed to connect to Wooting keyboard. Please ensure the device is connected.",
                        "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                _effectManager.StopCurrentEffect();
                _analogInputService.Shutdown();
                _keyboardService.Shutdown();

                StatusText.Text = "Not Connected";
                StatusText.Foreground = new SolidColorBrush(Colors.Black);
                DeviceCountText.Text = "";
                ConnectButton.Content = "Connect";
                EnableEffectButtons(false);
                ClearParametersPanel();
            }
        }

        private void EnableEffectButtons(bool enabled)
        {
            foreach (var child in EffectButtonsPanel.Children)
            {
                if (child is Button button)
                {
                    button.IsEnabled = enabled;
                }
            }
        }

        private void EffectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is IRGBEffect effect)
            {
                _effectManager.SetEffect(effect);
                EffectDescriptionText.Text = effect.Description;

                // Highlight selected button
                foreach (var child in EffectButtonsPanel.Children)
                {
                    if (child is Button btn)
                    {
                        btn.Background = btn == button ? new SolidColorBrush(Colors.LightBlue) : null;
                    }
                }
            }
        }

        private void OnEffectChanged(object? sender, IRGBEffect? effect)
        {
            Dispatcher.Invoke(() =>
            {
                ClearParametersPanel();

                if (effect == null)
                    return;

                NoParametersText.Visibility = Visibility.Collapsed;

                foreach (var parameter in effect.Parameters)
                {
                    var parameterControl = CreateParameterControl(parameter);
                    if (parameterControl != null)
                    {
                        ParametersPanel.Children.Add(parameterControl);
                    }
                }
            });
        }

        private UIElement? CreateParameterControl(IEffectParameter parameter)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

            var label = new TextBlock
            {
                Text = parameter.DisplayName,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            panel.Children.Add(label);

            switch (parameter.ParameterType)
            {
                case EffectParameterType.Color:
                    if (parameter is ColorParameter colorParam)
                    {
                        var colorPanel = new StackPanel { Orientation = Orientation.Horizontal };
                        
                        var colorDisplay = new Border
                        {
                            Width = 50,
                            Height = 30,
                            Background = new SolidColorBrush(colorParam.ColorValue),
                            BorderBrush = new SolidColorBrush(Colors.Black),
                            BorderThickness = new Thickness(1),
                            Margin = new Thickness(0, 0, 10, 0)
                        };

                        var colorButton = new Button
                        {
                            Content = "Choose Color",
                            Width = 100,
                            Height = 30
                        };

                        colorButton.Click += (s, e) =>
                        {
                            var dialog = new ColorPickerDialog(colorParam.ColorValue)
                            {
                                Owner = this
                            };

                            // Subscribe to live color changes
                            dialog.ColorChanged += (sender, newColor) =>
                            {
                                colorParam.Value = newColor;
                                colorDisplay.Background = new SolidColorBrush(newColor);
                            };

                            // Show dialog - no need to check DialogResult anymore
                            dialog.ShowDialog();
                            
                            // Apply final color when closed
                            colorParam.Value = dialog.SelectedColor;
                            colorDisplay.Background = new SolidColorBrush(dialog.SelectedColor);
                        };

                        colorPanel.Children.Add(colorDisplay);
                        colorPanel.Children.Add(colorButton);
                        panel.Children.Add(colorPanel);
                    }
                    break;

                case EffectParameterType.Speed:
                case EffectParameterType.Intensity:
                case EffectParameterType.Size:
                case EffectParameterType.Direction:
                    if (parameter is RangeParameter rangeParam)
                    {
                        var sliderPanel = new StackPanel();
                        
                        var valueText = new TextBlock
                        {
                            Text = $"{rangeParam.NumericValue:F0}",
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Margin = new Thickness(0, 0, 0, 3)
                        };

                        var slider = new Slider
                        {
                            Minimum = (double)rangeParam.MinValue,
                            Maximum = (double)rangeParam.MaxValue,
                            Value = rangeParam.NumericValue,
                            TickFrequency = 1,
                            IsSnapToTickEnabled = true
                        };

                        slider.ValueChanged += (s, e) =>
                        {
                            rangeParam.Value = e.NewValue;
                            valueText.Text = $"{e.NewValue:F0}";
                        };

                        sliderPanel.Children.Add(valueText);
                        sliderPanel.Children.Add(slider);
                        panel.Children.Add(sliderPanel);
                    }
                    break;
            }

            return panel;
        }

        private void ClearParametersPanel()
        {
            ParametersPanel.Children.Clear();
            NoParametersText.Visibility = Visibility.Visible;
            ParametersPanel.Children.Add(NoParametersText);
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _effectManager.Shutdown();
            _analogInputService.Shutdown();
            _keyboardService.Shutdown();
        }
    }
}