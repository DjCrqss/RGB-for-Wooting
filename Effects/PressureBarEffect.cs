using System.Windows.Media;
using System.Collections.Concurrent;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;
using MediaColor = System.Windows.Media.Color;

namespace WootingRGB.Effects;

public class PressureBarEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly ConcurrentDictionary<(int row, int col), double> _keyPressures = new();

    public override string Name => "Pressure Bars";
    public override string Description => "Bars grow from edges toward pressed keys based on pressure";

    public PressureBarEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "barColor",
            "Bar Color",
            MediaColor.FromRgb(0x28, 0xFF, 0x00) // #28FF00 - Bright Green
        ));

        _parameters.Add(new ColorParameter(
            "backgroundColor",
            "Background Color",
            MediaColor.FromRgb(0x03, 0x14, 0x00) // #031400 - Dark Green
        ));

        _parameters.Add(new ChoiceParameter(
            "direction",
            "Direction",
            new[] { "Vertical", "Horizontal", "Both" },
            "Both"
        ));

        _parameters.Add(new BooleanParameter(
            "inverted",
            "Inverted",
            defaultValue: false
        ));

        _parameters.Add(new RangeParameter(
            "fadeSpeed",
            "Fade Speed",
            EffectParameterType.Speed,
            defaultValue: 30,
            minValue: 1,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "sensitivity",
            "Sensitivity",
            EffectParameterType.Intensity,
            defaultValue: 100,
            minValue: 1,
            maxValue: 200
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _keyPressures.Clear();
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var barColor = GetParameter<ColorParameter>("barColor")?.ColorValue ?? MediaColor.FromRgb(0, 255, 255);
        var backgroundColor = GetParameter<ColorParameter>("backgroundColor")?.ColorValue ?? MediaColor.FromRgb(0, 0, 0);
        var direction = GetParameter<ChoiceParameter>("direction")?.StringValue ?? "Vertical";
        var inverted = GetParameter<BooleanParameter>("inverted")?.BooleanValue ?? false;
        var fadeSpeed = GetParameter<RangeParameter>("fadeSpeed")?.NumericValue ?? 30;
        var sensitivity = GetParameter<RangeParameter>("sensitivity")?.NumericValue ?? 100;

        bool isInverted = inverted;
        double fadeRate = (fadeSpeed / 100.0) * 0.05;
        double sensitivityMultiplier = sensitivity / 100.0;

        // Update pressed keys from analog input
        foreach (var pressedKey in keyboardState.PressedKeys)
        {
            var keyCode = pressedKey.Key;
            var pressure = Math.Clamp(pressedKey.Value * sensitivityMultiplier, 0, 2);

            if (WootingAnalogLedMapping.HidToPosition.TryGetValue(keyCode, out var position))
            {
                var (row, col) = position;

                if (row >= 0 && row < _keyboardService.MaxRows &&
                    col >= 0 && col < _keyboardService.MaxColumns)
                {
                    _keyPressures[position] = pressure;
                }
            }
        }

        // Fade out keys that are no longer pressed
        var keysToUpdate = new List<((int, int) key, double newValue)>();
        var keysToRemove = new List<(int, int)>();

        foreach (var kvp in _keyPressures)
        {
            // Check if key is still pressed
            bool stillPressed = false;
            foreach (var pressedKey in keyboardState.PressedKeys)
            {
                if (WootingAnalogLedMapping.HidToPosition.TryGetValue(pressedKey.Key, out var position) && position == kvp.Key)
                {
                    stillPressed = true;
                    break;
                }
            }

            if (!stillPressed)
            {
                var newPressure = kvp.Value - fadeRate;
                if (newPressure <= 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
                else
                {
                    keysToUpdate.Add((kvp.Key, newPressure));
                }
            }
        }

        foreach (var (key, value) in keysToUpdate)
        {
            _keyPressures[key] = value;
        }

        foreach (var key in keysToRemove)
        {
            _keyPressures.TryRemove(key, out _);
        }

        // Render the effect
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                double maxIntensity = 0;

                foreach (var pressedKeyPos in _keyPressures)
                {
                    var (pressRow, pressCol) = pressedKeyPos.Key;
                    var pressure = pressedKeyPos.Value;

                    double intensity = 0;

                    switch (direction)
                    {
                        case "Vertical":
                            intensity = CalculateVerticalBarIntensity(row, col, pressRow, pressCol, pressure, inverted);
                            break;
                        case "Horizontal":
                            intensity = CalculateHorizontalBarIntensity(row, col, pressRow, pressCol, pressure, inverted);
                            break;
                        case "Both":
                            var verticalIntensity = CalculateVerticalBarIntensity(row, col, pressRow, pressCol, pressure, inverted);
                            var horizontalIntensity = CalculateHorizontalBarIntensity(row, col, pressRow, pressCol, pressure, inverted);
                            intensity = Math.Max(verticalIntensity, horizontalIntensity);
                            break;
                    }

                    maxIntensity = Math.Max(maxIntensity, intensity);
                }

                // Blend bar color with background based on intensity
                MediaColor finalColor;
                if (maxIntensity > 0)
                {
                    finalColor = MediaColor.FromRgb(
                        (byte)(backgroundColor.R * (1 - maxIntensity) + barColor.R * maxIntensity),
                        (byte)(backgroundColor.G * (1 - maxIntensity) + barColor.G * maxIntensity),
                        (byte)(backgroundColor.B * (1 - maxIntensity) + barColor.B * maxIntensity)
                    );
                }
                else
                {
                    finalColor = backgroundColor;
                }

                _colorBuffer[row, col] = new KeyColour(finalColor.R, finalColor.G, finalColor.B);
            }
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    private double CalculateVerticalBarIntensity(int row, int col, int pressRow, int pressCol, double pressure, bool inverted)
    {
        // Only affect keys in the same column as the pressed key
        if (col != pressCol)
            return 0;

        if (inverted)
        {
            // Bars grow outward from the pressed key
            var distance = Math.Abs(row - pressRow);
            var maxDistance = Math.Max(pressRow, RGBControl.MaxRGBRows - pressRow);
            
            if (maxDistance == 0)
                return pressure;

            var normalizedDistance = (double)distance / maxDistance;
            
            // Bar reaches distance based on pressure
            if (normalizedDistance <= pressure)
            {
                return 1.0 - (normalizedDistance / pressure);
            }
            return 0;
        }
        else
        {
            // Bars grow inward toward the pressed key from edges
            var maxRows = RGBControl.MaxRGBRows;

            if (maxRows == 0)
                return pressure;

            // Calculate which side to check based on position relative to pressed key
            if (row <= pressRow)
            {
                // Check progress from top
                var distanceFromTop = (double)row + 1;
                var distanceToPressedKey = (double)pressRow + 1;
                var topReach = pressure * distanceToPressedKey;
                
                if (distanceFromTop <= topReach)
                {
                    // Calculate intensity with smooth falloff
                    var progressRatio = distanceFromTop / topReach;
                    return Math.Clamp(1.0 - progressRatio, 0, 1);
                }
            }
            else
            {
                // Check progress from bottom
                var distanceFromBottom = maxRows - row;
                var distanceToPressedKey = maxRows - pressRow;
                var bottomReach = pressure * distanceToPressedKey;
                
                if (distanceFromBottom <= bottomReach)
                {
                    // Calculate intensity with smooth falloff
                    var progressRatio = distanceFromBottom / bottomReach;
                    return Math.Clamp(1.0 - progressRatio, 0, 1);
                }
            }

            return 0;
        }
    }

    private double CalculateHorizontalBarIntensity(int row, int col, int pressRow, int pressCol, double pressure, bool inverted)
    {
        // Only affect keys in the same row as the pressed key
        if (row != pressRow)
            return 0;

        if (inverted)
        {
            // Bars grow outward from the pressed key
            var distance = Math.Abs(col - pressCol);
            var maxDistance = Math.Max(pressCol, RGBControl.MaxRGBCols - pressCol);
            
            if (maxDistance == 0)
                return pressure;

            var normalizedDistance = (double)distance / maxDistance;
            
            // Bar reaches distance based on pressure
            if (normalizedDistance <= pressure)
            {
                return 1.0 - (normalizedDistance / pressure);
            }
            return 0;
        }
        else
        {
            // Bars grow inward toward the pressed key from edges
            var maxCols = RGBControl.MaxRGBCols;

            if (maxCols == 0)
                return pressure;

            // Calculate which side to check based on position relative to pressed key
            if (col <= pressCol)
            {
                // Check progress from left
                var distanceFromLeft = (double)col + 1;
                var distanceToPressedKey = (double)pressCol + 1;
                var leftReach = pressure * distanceToPressedKey;
                
                if (distanceFromLeft <= leftReach)
                {
                    var progressRatio = distanceFromLeft / leftReach;
                    return Math.Clamp(1.0 - progressRatio, 0, 1);
                }
            }
            else
            {
                // Check progress from right
                var distanceFromRight = maxCols - col;
                var distanceToPressedKey = maxCols - pressCol;
                var rightReach = pressure * distanceToPressedKey;
                
                if (distanceFromRight <= rightReach)
                {
                    // Calculate intensity with smooth falloff
                    var progressRatio = distanceFromRight / rightReach;
                    return Math.Clamp(1.0 - progressRatio, 0, 1);
                }
            }

            return 0;
        }
    }

    public override void Cleanup()
    {
        _keyPressures.Clear();
    }
}
