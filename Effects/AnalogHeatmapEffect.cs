using System.Windows.Media;
using System.Collections.Concurrent;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;
using MediaColor = System.Windows.Media.Color;

namespace WootingRGB.Effects;

public class AnalogHeatmapEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private double[,]? _heatLevels;
    private readonly ConcurrentDictionary<(int row, int col), double> _lastPressures = new();

    public override string Name => "Heatmap";
    public override string Description => "Heatmap that accumulates analog key press intensity over time";

    public AnalogHeatmapEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new RangeParameter(
            "radius",
            "Radius",
            EffectParameterType.Size,
            defaultValue: 4,
            minValue: 0,
            maxValue: 5
        ));

        _parameters.Add(new RangeParameter(
            "decaySpeed",
            "Decay Speed",
            EffectParameterType.Speed,
            defaultValue: 100,
            minValue: 1,
            maxValue: 200
        ));

        _parameters.Add(new RangeParameter(
            "sensitivity",
            "Sensitivity",
            EffectParameterType.Intensity,
            defaultValue: 20,
            minValue: 1,
            maxValue: 40
        ));

        _parameters.Add(new ColorParameter(
            "backgroundColor",
            "Background Color",
            MediaColor.FromRgb(0x00, 0x0A, 0x72) // #000A72 - Dark blue
        ));

        _parameters.Add(new ColorParameter(
            "coolColor",
            "Cool Color",
            MediaColor.FromRgb(0x7D, 0xFF, 0x00) // #7DFF00 - Bright lime green
        ));

        _parameters.Add(new ColorParameter(
            "hotColor",
            "Hot Color",
            MediaColor.FromRgb(0xFF, 0x00, 0x00) // #FF0000 - Red
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _heatLevels = new double[_keyboardService.MaxRows, _keyboardService.MaxColumns];
        _lastPressures.Clear();
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null || _heatLevels == null) return;

        var radius = (int)Math.Round(GetParameter<RangeParameter>("radius")?.NumericValue ?? 2);
        var decaySpeed = GetParameter<RangeParameter>("decaySpeed")?.NumericValue ?? 50;
        var sensitivity = GetParameter<RangeParameter>("sensitivity")?.NumericValue ?? 25;
        var backgroundColor = GetParameter<ColorParameter>("backgroundColor")?.ColorValue ?? MediaColor.FromRgb(0, 0, 32);
        var coolColor = GetParameter<ColorParameter>("coolColor")?.ColorValue ?? MediaColor.FromRgb(0, 128, 255);
        var hotColor = GetParameter<ColorParameter>("hotColor")?.ColorValue ?? MediaColor.FromRgb(255, 0, 0);

        double decayRate = (decaySpeed / 100.0) * 0.01;
        double sensitivityMultiplier = sensitivity / 100.0;

        // Update pressed keys
        foreach (var pressedKey in keyboardState.PressedKeys)
        {
            var keyCode = pressedKey.Key;
            var pressure = Math.Clamp(pressedKey.Value, 0, 1);

            if (WootingAnalogLedMapping.HidToPosition.TryGetValue(keyCode, out var position))
            {
                var (row, col) = position;

                if (row >= 0 && row < _keyboardService.MaxRows &&
                    col >= 0 && col < _keyboardService.MaxColumns)
                {
                    var lastPressure = _lastPressures.GetValueOrDefault(position, 0);
                    
                    // If pressure increased, add the difference to heat levels with radius falloff
                    if (pressure > lastPressure)
                    {
                        var pressureDiff = (pressure - lastPressure) * sensitivityMultiplier;
                        ApplyHeatWithRadius(row, col, pressureDiff, radius);
                    }

                    // Update last pressure
                    _lastPressures[position] = pressure;
                }
            }
        }

        // Remove keys that are no longer pressed
        var currentKeys = new HashSet<(int, int)>();
        foreach (var pressedKey in keyboardState.PressedKeys)
        {
            if (WootingAnalogLedMapping.HidToPosition.TryGetValue(pressedKey.Key, out var position))
            {
                currentKeys.Add(position);
            }
        }

        var keysToRemove = _lastPressures.Keys.Where(k => !currentKeys.Contains(k)).ToList();
        foreach (var key in keysToRemove)
        {
            _lastPressures.TryRemove(key, out _);
        }

        // Apply decay to all heat levels
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                _heatLevels[row, col] = Math.Max(0, _heatLevels[row, col] - decayRate);
            }
        }

        // Render the heatmap with three-color gradient
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                var heatLevel = Math.Clamp(_heatLevels[row, col], 0, 1);
                
                MediaColor finalColor;
                if (heatLevel <= 0.5)
                {
                    // Interpolate from background to cool color (0.0 to 0.5)
                    var t = heatLevel * 2.0; // Remap 0-0.5 to 0-1
                    finalColor = EffectUtilities.LerpColor(backgroundColor, coolColor, t);
                }
                else
                {
                    // Interpolate from cool to hot color (0.5 to 1.0)
                    var t = (heatLevel - 0.5) * 2.0; // Remap 0.5-1.0 to 0-1
                    finalColor = EffectUtilities.LerpColor(coolColor, hotColor, t);
                }

                _colorBuffer[row, col] = new KeyColour(
                    finalColor.R,
                    finalColor.G,
                    finalColor.B
                );
            }
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    private void ApplyHeatWithRadius(int centerRow, int centerCol, double amount, int radius)
    {
        if (_heatLevels == null) return;

        for (int row = Math.Max(0, centerRow - radius); row <= Math.Min(_keyboardService.MaxRows - 1, centerRow + radius); row++)
        {
            for (int col = Math.Max(0, centerCol - radius); col <= Math.Min(_keyboardService.MaxColumns - 1, centerCol + radius); col++)
            {
                if (radius == 0)
                {
                    // No falloff, just apply to center key
                    if (row == centerRow && col == centerCol)
                    {
                        _heatLevels[row, col] = Math.Min(1.0, _heatLevels[row, col] + amount);
                    }
                }
                else
                {
                    // Calculate distance and falloff
                    var distance = Math.Sqrt(Math.Pow(row - centerRow, 2) + Math.Pow(col - centerCol, 2));
                    
                    if (distance <= radius)
                    {
                        // Linear falloff from center to radius
                        var falloff = 1.0 - (distance / radius);
                        var heatToAdd = amount * falloff;
                        _heatLevels[row, col] = Math.Min(1.0, _heatLevels[row, col] + heatToAdd);
                    }
                }
            }
        }
    }

    public override void Cleanup()
    {
        _lastPressures.Clear();
        if (_heatLevels != null)
        {
            Array.Clear(_heatLevels);
        }
    }
}
