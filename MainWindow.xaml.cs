using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
            
            // Start with effect buttons disabled
            EnableEffectButtons(false);
        }

        private void InitializeEffectButtons()
        {
            // Define effect categories
            var categories = new Dictionary<string, List<IRGBEffect>>
            {
                ["Basic"] = new List<IRGBEffect>(),
                ["Advanced"] = new List<IRGBEffect>(),
                ["Hall Effect"] = new List<IRGBEffect>()
            };

            // Categorize effects
            foreach (var effect in _effectManager.AvailableEffects)
            {
                switch (effect.Name)
                {
                    case "Static Color":
                    case "Rainbow":
                    case "Rain":
                    case "Sparkles":
                    case "Breathing":
                        categories["Basic"].Add(effect);
                        break;
                    case "Fire":
                        categories["Advanced"].Add(effect);
                        break;
                    case "Perlin Noise":
                    case "Ripple":
                    case "Pressure Bars":
                    case "Joystick":
                        categories["Hall Effect"].Add(effect);
                        break;
                    default:
                        // If an effect doesn't match, put it in Advanced
                        categories["Advanced"].Add(effect);
                        break;
                }
            }

            // Create collapsible sections for each category
            foreach (var category in categories)
            {
                if (category.Value.Count == 0) continue;

                // Create expander for category
                var expander = new Expander
                {
                    Header = category.Key,
                    IsExpanded = true,
                    Margin = new Thickness(0, 0, 0, 10),
                    Style = (Style)FindResource("CategoryExpander")
                };

                // Create grid for effect buttons in this category
                var effectsGrid = new UniformGrid
                {
                    Columns = 2,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                // Add effect buttons to the grid
                foreach (var effect in category.Value)
                {
                    var button = new Button
                    {
                        Content = effect.Name,
                        Tag = effect,
                        Style = (Style)FindResource("EffectButton"),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"))
                    };

                    button.Click += EffectButton_Click;
                    effectsGrid.Children.Add(button);
                }

                expander.Content = effectsGrid;
                EffectCategoriesPanel.Children.Add(expander);
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
                    _effectManager.Enable();
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
                _effectManager.Disable();
                _analogInputService.Shutdown();
                _keyboardService.Shutdown();

                StatusText.Text = "Not connected";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
                DeviceCountText.Text = "";
                ConnectButton.Content = "Connect";
                EnableEffectButtons(false);
                ClearParametersPanel();
                
                // Clear selected effect button highlight in all categories
                foreach (var child in EffectCategoriesPanel.Children)
                {
                    if (child is Expander expander && expander.Content is UniformGrid grid)
                    {
                        foreach (var gridChild in grid.Children)
                        {
                            if (gridChild is Button btn)
                            {
                                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"));
                                btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
                            }
                        }
                    }
                }
            }
        }

        private void EnableEffectButtons(bool enabled)
        {
            foreach (var child in EffectCategoriesPanel.Children)
            {
                if (child is Expander expander && expander.Content is UniformGrid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is Button button)
                        {
                            button.IsEnabled = enabled;
                        }
                    }
                }
            }
        }

        private void EffectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_keyboardService.IsInitialized)
            {
                return;
            }

            if (sender is Button button && button.Tag is IRGBEffect effect)
            {
                _effectManager.SetEffect(effect);

                // Highlight selected button with yellow accent color and black text
                foreach (var child in EffectCategoriesPanel.Children)
                {
                    if (child is Expander expander && expander.Content is UniformGrid grid)
                    {
                        foreach (var gridChild in grid.Children)
                        {
                            if (gridChild is Button btn)
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
                case EffectParameterType.Boolean:
                    if (parameter is BooleanParameter boolParam)
                    {
                        var togglePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
                        var toggleRow = new Grid();
                        toggleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        toggleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        
                        var label = new TextBlock
                        {
                            Text = parameter.DisplayName,
                            FontSize = 13,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 15, 0)
                        };
                        Grid.SetColumn(label, 0);
                        
                        var toggleSwitch = new ToggleSwitch
                        {
                            IsChecked = boolParam.BooleanValue,
                            HorizontalAlignment = HorizontalAlignment.Left
                        };

                        toggleSwitch.Toggled += (s, isChecked) =>
                        {
                            boolParam.Value = isChecked;
                        };

                        Grid.SetColumn(toggleSwitch, 1);
                        toggleRow.Children.Add(label);
                        toggleRow.Children.Add(toggleSwitch);
                        togglePanel.Children.Add(toggleRow);
                        return togglePanel;
                    }
                    break;

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

                case EffectParameterType.Direction:
                    if (parameter is ChoiceParameter choiceParam)
                    {
                        var choicePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
                        
                        var label = new TextBlock
                        {
                            Text = parameter.DisplayName,
                            FontSize = 13,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                            Margin = new Thickness(0, 0, 0, 10)
                        };
                        choicePanel.Children.Add(label);

                        var comboBox = new ComboBox
                        {
                            ItemsSource = choiceParam.Choices,
                            SelectedItem = choiceParam.StringValue,
                            FontSize = 13,
                            Height = 35
                        };

                        // Create custom template for dark theme ComboBox
                        var comboBoxTemplate = new ControlTemplate(typeof(ComboBox));
                        
                        // Main grid
                        var mainGrid = new FrameworkElementFactory(typeof(Grid));
                        mainGrid.SetValue(Grid.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")));
                        
                        // Define columns
                        var column1 = new FrameworkElementFactory(typeof(ColumnDefinition));
                        column1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
                        var column2 = new FrameworkElementFactory(typeof(ColumnDefinition));
                        column2.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
                        mainGrid.AppendChild(column1);
                        mainGrid.AppendChild(column2);
                        
                        // Border around everything
                        var mainBorder = new FrameworkElementFactory(typeof(Border));
                        mainBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
                        mainBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555")));
                        mainBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                        mainBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
                        mainBorder.SetValue(Grid.ColumnProperty, 0);
                        mainBorder.SetValue(Grid.ColumnSpanProperty, 2);
                        
                        // Toggle button (invisible, covers whole area)
                        var toggleButton = new FrameworkElementFactory(typeof(ToggleButton));
                        toggleButton.SetValue(ToggleButton.NameProperty, "toggleButton");
                        toggleButton.SetValue(Grid.ColumnProperty, 0);
                        toggleButton.SetValue(Grid.ColumnSpanProperty, 2);
                        toggleButton.SetValue(ToggleButton.BackgroundProperty, Brushes.Transparent);
                        toggleButton.SetValue(ToggleButton.BorderBrushProperty, Brushes.Transparent);
                        toggleButton.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsDropDownOpen") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent), Mode = BindingMode.TwoWay });
                        toggleButton.SetValue(ToggleButton.FocusableProperty, false);
                        toggleButton.SetValue(ToggleButton.ClickModeProperty, ClickMode.Press);
                        toggleButton.SetValue(ToggleButton.CursorProperty, Cursors.Hand);
                        
                        // Content presenter (selected item display)
                        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                        contentPresenter.SetValue(ContentPresenter.NameProperty, "ContentSite");
                        contentPresenter.SetValue(Grid.ColumnProperty, 0);
                        contentPresenter.SetValue(ContentPresenter.MarginProperty, new Thickness(10, 0, 0, 0));
                        contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                        contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                        contentPresenter.SetValue(ContentPresenter.IsHitTestVisibleProperty, false);
                        contentPresenter.SetBinding(ContentPresenter.ContentProperty, new Binding("SelectionBoxItem") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
                        
                        // Dropdown arrow container
                        var arrowBorder = new FrameworkElementFactory(typeof(Border));
                        arrowBorder.SetValue(Grid.ColumnProperty, 1);
                        arrowBorder.SetValue(Border.WidthProperty, 30.0);
                        arrowBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E3E42")));
                        arrowBorder.SetValue(Border.IsHitTestVisibleProperty, false);
                        
                        var arrowPath = new FrameworkElementFactory(typeof(Path));
                        arrowPath.SetValue(Path.DataProperty, Geometry.Parse("M 0 0 L 4 4 L 8 0 Z"));
                        arrowPath.SetValue(Path.FillProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")));
                        arrowPath.SetValue(Path.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                        arrowPath.SetValue(Path.VerticalAlignmentProperty, VerticalAlignment.Center);
                        
                        arrowBorder.AppendChild(arrowPath);
                        
                        // Popup
                        var popup = new FrameworkElementFactory(typeof(Popup));
                        popup.SetValue(Popup.NameProperty, "PART_Popup");
                        popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
                        popup.SetValue(Popup.AllowsTransparencyProperty, true);
                        popup.SetBinding(Popup.IsOpenProperty, new Binding("IsDropDownOpen") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
                        
                        var popupBorder = new FrameworkElementFactory(typeof(Border));
                        popupBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")));
                        popupBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555")));
                        popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                        popupBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
                        popupBorder.SetValue(Border.MarginProperty, new Thickness(0, 1, 0, 0));
                        popupBorder.SetBinding(Border.MinWidthProperty, new Binding("ActualWidth") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
                        popupBorder.SetBinding(Border.MaxHeightProperty, new Binding("MaxDropDownHeight") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
                        
                        var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
                        scrollViewer.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
                        
                        var itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
                        itemsPresenter.SetValue(ItemsPresenter.NameProperty, "ItemsPresenter");
                        itemsPresenter.SetValue(KeyboardNavigation.DirectionalNavigationProperty, KeyboardNavigationMode.Contained);
                        
                        scrollViewer.AppendChild(itemsPresenter);
                        popupBorder.AppendChild(scrollViewer);
                        popup.AppendChild(popupBorder);
                        
                        mainGrid.AppendChild(mainBorder);
                        mainGrid.AppendChild(toggleButton);
                        mainGrid.AppendChild(contentPresenter);
                        mainGrid.AppendChild(arrowBorder);
                        mainGrid.AppendChild(popup);
                        
                        comboBoxTemplate.VisualTree = mainGrid;
                        
                        // Add trigger for hover effect on Grid background
                        var mouseOverTrigger = new Trigger { Property = ComboBox.IsMouseOverProperty, Value = true };
                        mouseOverTrigger.Setters.Add(new Setter(Grid.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E3E42"))));
                        comboBoxTemplate.Triggers.Add(mouseOverTrigger);
                        
                        comboBox.Template = comboBoxTemplate;
                        
                        // Apply text color
                        comboBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
                        
                        // Style for ComboBox items
                        var itemStyle = new Style(typeof(ComboBoxItem));
                        itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"))));
                        itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"))));
                        itemStyle.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(10, 8, 10, 8)));
                        itemStyle.Setters.Add(new Setter(ComboBoxItem.BorderThicknessProperty, new Thickness(0)));
                        
                        var itemHoverTrigger = new Trigger { Property = ComboBoxItem.IsMouseOverProperty, Value = true };
                        itemHoverTrigger.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E3E42"))));
                        itemStyle.Triggers.Add(itemHoverTrigger);
                        
                        var selectedTrigger = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
                        selectedTrigger.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD45C"))));
                        selectedTrigger.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, new SolidColorBrush(Colors.Black)));
                        itemStyle.Triggers.Add(selectedTrigger);
                        
                        comboBox.ItemContainerStyle = itemStyle;

                        comboBox.SelectionChanged += (s, e) =>
                        {
                            if (comboBox.SelectedItem is string selectedValue)
                            {
                                choiceParam.Value = selectedValue;
                            }
                        };

                        choicePanel.Children.Add(comboBox);
                        return choicePanel;
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

        private void Expander_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.TemplatedParent is Expander expander)
            {
                expander.IsExpanded = !expander.IsExpanded;
                e.Handled = true;
            }
        }
    }
}