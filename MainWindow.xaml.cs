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
                    Tag = effect,
                    Style = (Style)FindResource("EffectButton"),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"))
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
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4EC9B0"));
                    DeviceCountText.Text = $"{_keyboardService.DeviceCount} device(s) found";
                    ConnectButton.Content = "Disconnect";
                    EnableEffectButtons(true);
                }
                else
                {
                    StatusText.Text = "Connection Failed";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F48771"));
                    MessageBox.Show("Failed to connect to Wooting keyboard. Please ensure the device is connected.",
                        "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                _effectManager.StopCurrentEffect();
                _analogInputService.Shutdown();
                _keyboardService.Shutdown();

                StatusText.Text = "Not connected";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
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

                // Highlight selected button with yellow accent color and black text
                foreach (var child in EffectButtonsPanel.Children)
                {
                    if (child is Button btn)
                    {
                        if (btn == button)
                        {
                            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD45C"));
                            btn.Foreground = new SolidColorBrush(Colors.Black);
                        }
                        else
                        {
                            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"));
                            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
                        }
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
            switch (parameter.ParameterType)
            {
                case EffectParameterType.Color:
                    if (parameter is ColorParameter colorParam)
                    {
                        var colorPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
                        var colorRow = new Grid();
                        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        
                        var label = new TextBlock
                        {
                            Text = parameter.DisplayName,
                            FontSize = 13,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 15, 0)
                        };
                        Grid.SetColumn(label, 0);
                        
                        var colorButton = new Button
                        {
                            Width = 40,
                            Height = 40,
                            Background = new SolidColorBrush(colorParam.ColorValue),
                            BorderBrush = new SolidColorBrush(Colors.White),
                            BorderThickness = new Thickness(2),
                            Cursor = Cursors.Hand,
                            HorizontalAlignment = HorizontalAlignment.Left
                        };

                        // Create circular button style
                        var buttonTemplate = new ControlTemplate(typeof(Button));
                        var border = new FrameworkElementFactory(typeof(Border));
                        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
                        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
                        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(20));
                        buttonTemplate.VisualTree = border;
                        colorButton.Template = buttonTemplate;

                        colorButton.Click += (s, e) =>
                        {
                            var dialog = new ColorPickerDialog(colorParam.ColorValue)
                            {
                                Owner = this
                            };

                            dialog.ColorChanged += (sender, newColor) =>
                            {
                                colorParam.Value = newColor;
                                colorButton.Background = new SolidColorBrush(newColor);
                            };

                            dialog.ShowDialog();
                            
                            colorParam.Value = dialog.SelectedColor;
                            colorButton.Background = new SolidColorBrush(dialog.SelectedColor);
                        };

                        Grid.SetColumn(colorButton, 1);
                        colorRow.Children.Add(label);
                        colorRow.Children.Add(colorButton);
                        colorPanel.Children.Add(colorRow);
                        return colorPanel;
                    }
                    break;

                case EffectParameterType.Speed:
                case EffectParameterType.Intensity:
                case EffectParameterType.Size:
                case EffectParameterType.Direction:
                    if (parameter is RangeParameter rangeParam)
                    {
                        var sliderPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
                        
                        // Top row: Label
                        var label = new TextBlock
                        {
                            Text = parameter.DisplayName,
                            FontSize = 13,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                            Margin = new Thickness(0, 0, 0, 10)
                        };
                        sliderPanel.Children.Add(label);

                        // Bottom row: Slider, TextBox, and % symbol
                        var controlRow = new Grid();
                        controlRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        controlRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        controlRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var slider = new Slider
                        {
                            Minimum = (double)rangeParam.MinValue,
                            Maximum = (double)rangeParam.MaxValue,
                            Value = rangeParam.NumericValue,
                            TickFrequency = 1,
                            IsSnapToTickEnabled = true,
                            Style = (Style)FindResource("EffectSlider"),
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 15, 0)
                        };
                        Grid.SetColumn(slider, 0);

                        // Create a border for rounded corners
                        var textBoxBorder = new Border
                        {
                            Width = 50,
                            Height = 30,
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555")),
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(4),
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 5, 0)
                        };

                        var valueTextBox = new TextBox
                        {
                            Text = $"{rangeParam.NumericValue:F0}",
                            Background = Brushes.Transparent,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                            BorderThickness = new Thickness(0),
                            Padding = new Thickness(8, 5, 8, 5),
                            FontSize = 13,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextAlignment = TextAlignment.Center
                        };

                        textBoxBorder.Child = valueTextBox;
                        Grid.SetColumn(textBoxBorder, 1);

                        var percentSymbol = new TextBlock
                        {
                            Text = "%",
                            FontSize = 13,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(percentSymbol, 2);

                        // Slider updates TextBox
                        slider.ValueChanged += (s, e) =>
                        {
                            rangeParam.Value = e.NewValue;
                            valueTextBox.Text = $"{e.NewValue:F0}";
                        };

                        // TextBox updates slider and parameter
                        valueTextBox.TextChanged += (s, e) =>
                        {
                            if (double.TryParse(valueTextBox.Text, out double value))
                            {
                                value = Math.Clamp(value, (double)rangeParam.MinValue, (double)rangeParam.MaxValue);
                                if (Math.Abs(slider.Value - value) > 0.01)
                                {
                                    slider.Value = value;
                                }
                            }
                        };

                        // Handle focus loss to validate and correct input
                        valueTextBox.LostFocus += (s, e) =>
                        {
                            if (!double.TryParse(valueTextBox.Text, out double value))
                            {
                                valueTextBox.Text = $"{slider.Value:F0}";
                            }
                            else
                            {
                                value = Math.Clamp(value, (double)rangeParam.MinValue, (double)rangeParam.MaxValue);
                                valueTextBox.Text = $"{value:F0}";
                                slider.Value = value;
                            }
                        };

                        controlRow.Children.Add(slider);
                        controlRow.Children.Add(textBoxBorder);
                        controlRow.Children.Add(percentSymbol);
                        sliderPanel.Children.Add(controlRow);
                        return sliderPanel;
                    }
                    break;
            }

            return null;
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