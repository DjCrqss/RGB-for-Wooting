using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;

namespace WootingRGB.Effects;

public class ReactiveEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly Dictionary<(int row, int col), double> _keyIntensities = new();
    private readonly Random _random = new();

    public override string Name => "Reactive";
    public override string Description => "Keys light up when pressed";

    public ReactiveEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "backgroundColor",
            "Background Color",
            Color.FromRgb(0x00, 0x00, 0x00) // Black
        ));

        _parameters.Add(new ColorParameter(
            "pressColor",
            "Press Color",
            Colors.Magenta
        ));

        _parameters.Add(new ColorParameter(
            "releaseColor",
            "Release Color",
            Colors.Purple
        ));

        _parameters.Add(new RangeParameter(
            "fadeSpeed",
            "Fade Speed",
            EffectParameterType.Speed,
            defaultValue: 50,
            minValue: 1,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "sensitivity",
            "Analog Sensitivity",
            EffectParameterType.Intensity,
            defaultValue: 50,
            minValue: 1,
            maxValue: 100
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _keyIntensities.Clear();
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var backgroundColor = GetParameter<ColorParameter>("backgroundColor")?.ColorValue ?? Colors.Black;
        var pressColor = GetParameter<ColorParameter>("pressColor")?.ColorValue ?? Colors.Magenta;
        var releaseColor = GetParameter<ColorParameter>("releaseColor")?.ColorValue ?? Colors.Purple;
        var fadeSpeed = GetParameter<RangeParameter>("fadeSpeed")?.NumericValue ?? 50;
        var sensitivity = GetParameter<RangeParameter>("sensitivity")?.NumericValue ?? 50;

        var fadeRate = fadeSpeed / 1000.0;

        // Update intensities based on pressed keys
        foreach (var key in keyboardState.PressedKeys)
        {
            // TODO: Convert keycode to row/col
            // For now, simulate with random positions
            var row = _random.Next(_keyboardService.MaxRows);
            var col = _random.Next(_keyboardService.MaxColumns);
            var pressure = Math.Min(key.Value * (sensitivity / 50.0), 1.0);
            
            _keyIntensities[(row, col)] = pressure;
        }

        // Fade out all keys
        var keysToRemove = new List<(int, int)>();
        foreach (var kvp in _keyIntensities.ToList())
        {
            var newIntensity = kvp.Value - fadeRate;
            if (newIntensity <= 0)
            {
                keysToRemove.Add(kvp.Key);
            }
            else
            {
                _keyIntensities[kvp.Key] = newIntensity;
            }
        }

        foreach (var key in keysToRemove)
        {
            _keyIntensities.Remove(key);
        }

        // Fill with background color
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                _colorBuffer[row, col] = new KeyColour(
                    backgroundColor.R,
                    backgroundColor.G,
                    backgroundColor.B
                );
            }
        }

        // Draw active keys on top of background
        foreach (var kvp in _keyIntensities)
        {
            var intensity = kvp.Value;
            var color = EffectUtilities.LerpColor(releaseColor, pressColor, intensity);

            _colorBuffer[kvp.Key.row, kvp.Key.col] = new KeyColour(
                (byte)(color.R * intensity),
                (byte)(color.G * intensity),
                (byte)(color.B * intensity)
            );
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    public override void Cleanup()
    {
        _keyIntensities.Clear();
    }
}
