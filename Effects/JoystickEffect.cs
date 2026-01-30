using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;
using MediaColor = System.Windows.Media.Color;

namespace WootingRGB.Effects;

public class JoystickEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private double _targetX = 0;
    private double _targetY = 0;
    private double _currentX = 0;
    private double _currentY = 0;

    // HID codes for directional keys
    private const short HID_W = 26;
    private const short HID_A = 4;
    private const short HID_S = 22;
    private const short HID_D = 7;
    private const short HID_ARROW_UP = 82;
    private const short HID_ARROW_LEFT = 80;
    private const short HID_ARROW_DOWN = 81;
    private const short HID_ARROW_RIGHT = 79;

    public override string Name => "Joystick";
    public override string Description => "Radial gradient centered on keyboard that shifts with WASD/Arrow keys like a giant joystick";

    public JoystickEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "centerColor",
            "Center Color",
            MediaColor.FromRgb(0xED, 0x56, 0x3A) // #ED563A - Coral/Orange-Red
        ));

        _parameters.Add(new ColorParameter(
            "edgeColor",
            "Edge Color",
            MediaColor.FromRgb(0x0B, 0x00, 0x30) // #0B0030 - Deep Purple/Black
        ));

        _parameters.Add(new ChoiceParameter(
            "pattern",
            "Pattern",
            new[] { "Radial", "Cross" },
            "Radial"
        ));

        _parameters.Add(new RangeParameter(
            "range",
            "Range",
            EffectParameterType.Intensity,
            defaultValue: 15,
            minValue: 10,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "smoothing",
            "Smoothing",
            EffectParameterType.Intensity,
            defaultValue: 70,
            minValue: 0,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "radius",
            "Gradient Radius",
            EffectParameterType.Intensity,
            defaultValue: 20,
            minValue: 10,
            maxValue: 200
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _targetX = 0;
        _targetY = 0;
        _currentX = 0;
        _currentY = 0;
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var centerColor = GetParameter<ColorParameter>("centerColor")?.ColorValue ?? MediaColor.FromRgb(255, 255, 255);
        var edgeColor = GetParameter<ColorParameter>("edgeColor")?.ColorValue ?? MediaColor.FromRgb(0, 0, 255);
        var pattern = GetParameter<ChoiceParameter>("pattern")?.StringValue ?? "Radial";
        var range = GetParameter<RangeParameter>("range")?.NumericValue ?? 50;
        var smoothing = GetParameter<RangeParameter>("smoothing")?.NumericValue ?? 70;
        var radius = GetParameter<RangeParameter>("radius")?.NumericValue ?? 50;

        double rangeMultiplier = range / 50.0;
        double smoothingFactor = smoothing / 100.0;
        double radiusMultiplier = radius / 50.0;

        // Calculate keyboard center in row/col space
        double keyboardCenterRow = _keyboardService.MaxRows / 2.0;
        double keyboardCenterCol = _keyboardService.MaxColumns / 2.0;

        // Get input from WASD and arrow keys - direct position control
        double inputX = 0;
        double inputY = 0;

        foreach (var key in keyboardState.PressedKeys)
        {
            var pressure = Math.Clamp(key.Value, 0, 1);
            
            switch (key.Key)
            {
                case HID_W:
                case HID_ARROW_UP:
                    inputY -= pressure;
                    break;
                case HID_S:
                case HID_ARROW_DOWN:
                    inputY += pressure;
                    break;
                case HID_A:
                case HID_ARROW_LEFT:
                    inputX -= pressure;
                    break;
                case HID_D:
                case HID_ARROW_RIGHT:
                    inputX += pressure;
                    break;
            }
        }

        // Scale input by range
        double maxOffset = Math.Max(_keyboardService.MaxRows, _keyboardService.MaxColumns) * rangeMultiplier;
        _targetX = inputX * maxOffset;
        _targetY = inputY * maxOffset;

        // Smooth interpolation to target position
        _currentX = _currentX * smoothingFactor + _targetX * (1 - smoothingFactor);
        _currentY = _currentY * smoothingFactor + _targetY * (1 - smoothingFactor);

        // Render the effect
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                double intensity;

                if (pattern == "Cross")
                {
                    intensity = CalculateCrossIntensity(row, col, keyboardCenterRow, keyboardCenterCol, radiusMultiplier);
                }
                else // Radial
                {
                    intensity = CalculateRadialIntensity(row, col, keyboardCenterRow, keyboardCenterCol, radiusMultiplier);
                }

                // Blend between center and edge color
                MediaColor finalColor = BlendColors(edgeColor, centerColor, intensity);
                _colorBuffer[row, col] = new KeyColour(finalColor.R, finalColor.G, finalColor.B);
            }
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    private double CalculateRadialIntensity(int row, int col, double centerRow, double centerCol, double radiusMultiplier)
    {
        // Calculate distance from the shifted center
        double offsetRow = centerRow + _currentY;
        double offsetCol = centerCol + _currentX;

        double dx = col - offsetCol;
        double dy = row - offsetRow;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        // Normalize by radius
        double maxRadius = Math.Max(_keyboardService.MaxRows, _keyboardService.MaxColumns) * radiusMultiplier;
        double normalizedDistance = distance / maxRadius;

        // Invert so center is bright (1.0) and edges are dim (0.0)
        double intensity = Math.Clamp(1.0 - normalizedDistance, 0, 1);
        
        // Apply smooth curve for better gradient
        intensity = Math.Pow(intensity, 1.5);

        return intensity;
    }

    private double CalculateCrossIntensity(int row, int col, double centerRow, double centerCol, double radiusMultiplier)
    {
        // Calculate shifted center position
        double offsetRow = centerRow + _currentY;
        double offsetCol = centerCol + _currentX;

        double dx = Math.Abs(col - offsetCol);
        double dy = Math.Abs(row - offsetRow);

        // Use Manhattan distance for cross pattern (minimum of horizontal and vertical distance)
        double crossDistance = Math.Min(dx, dy);

        // Normalize by radius
        double maxRadius = Math.Max(_keyboardService.MaxRows, _keyboardService.MaxColumns) * radiusMultiplier * 0.5;
        double normalizedDistance = crossDistance / maxRadius;

        // Invert so center is bright
        double intensity = Math.Clamp(1.0 - normalizedDistance, 0, 1);
        
        // Apply smooth curve
        intensity = Math.Pow(intensity, 2.0);

        return intensity;
    }

    private MediaColor BlendColors(MediaColor color1, MediaColor color2, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return MediaColor.FromRgb(
            (byte)(color1.R + (color2.R - color1.R) * t),
            (byte)(color1.G + (color2.G - color1.G) * t),
            (byte)(color1.B + (color2.B - color1.B) * t)
        );
    }

    public override void Cleanup()
    {
        _targetX = 0;
        _targetY = 0;
        _currentX = 0;
        _currentY = 0;
    }
}
