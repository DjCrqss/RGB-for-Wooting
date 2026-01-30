using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WootingRGB.Controls;

public partial class ToggleSwitch : UserControl
{
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsCheckedChanged));

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public event EventHandler<bool>? Toggled;

    public ToggleSwitch()
    {
        InitializeComponent();
        UpdateVisualState(false);
    }

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToggleSwitch toggle)
        {
            toggle.UpdateVisualState(true);
        }
    }

    private void Toggle_Click(object sender, MouseButtonEventArgs e)
    {
        IsChecked = !IsChecked;
        Toggled?.Invoke(this, IsChecked);
    }

    private void UpdateVisualState(bool animate)
    {
        double targetX = IsChecked ? 24 : 0; // 50 - 18 - 8 = 24 (track width - thumb width - padding)
        var trackColor = IsChecked 
            ? (Color)ColorConverter.ConvertFromString("#FFD45C") 
            : (Color)ColorConverter.ConvertFromString("#3E3E42");
        var thumbColor = IsChecked
            ? (Color)ColorConverter.ConvertFromString("#1E1E1E")
            : (Color)ColorConverter.ConvertFromString("#E0E0E0");

        if (animate)
        {
            var duration = TimeSpan.FromMilliseconds(200);
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

            // Animate thumb position
            var thumbAnimation = new DoubleAnimation
            {
                To = targetX,
                Duration = duration,
                EasingFunction = easing
            };
            ThumbTransform.BeginAnimation(TranslateTransform.XProperty, thumbAnimation);

            // Animate track color
            var trackColorAnimation = new ColorAnimation
            {
                To = trackColor,
                Duration = duration,
                EasingFunction = easing
            };
            var trackBrush = TrackBorder.Background as SolidColorBrush;
            if (trackBrush != null)
            {
                trackBrush.BeginAnimation(SolidColorBrush.ColorProperty, trackColorAnimation);
            }

            // Animate thumb color
            var thumbColorAnimation = new ColorAnimation
            {
                To = thumbColor,
                Duration = duration,
                EasingFunction = easing
            };
            var thumbBrush = Thumb.Fill as SolidColorBrush;
            if (thumbBrush != null)
            {
                thumbBrush.BeginAnimation(SolidColorBrush.ColorProperty, thumbColorAnimation);
            }

            // Animate checkmark
            var checkMarkAnimation = new DoubleAnimation
            {
                To = IsChecked ? 1.0 : 0.0,
                Duration = duration,
                EasingFunction = easing
            };
            CheckMark.BeginAnimation(OpacityProperty, checkMarkAnimation);

            var checkMarkPositionAnimation = new DoubleAnimation
            {
                To = targetX,
                Duration = duration,
                EasingFunction = easing
            };
            CheckMarkTransform.BeginAnimation(TranslateTransform.XProperty, checkMarkPositionAnimation);
        }
        else
        {
            ThumbTransform.X = targetX;
            TrackBorder.Background = new SolidColorBrush(trackColor);
            Thumb.Fill = new SolidColorBrush(thumbColor);
            CheckMark.Opacity = IsChecked ? 1.0 : 0.0;
            CheckMarkTransform.X = targetX;
        }
    }
}
