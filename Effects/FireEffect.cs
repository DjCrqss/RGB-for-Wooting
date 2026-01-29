using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;

namespace WootingRGB.Effects;

public class FireEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly Random _random = new();
    private double[,]? _heatMap;

    public override string Name => "Fire";
    public override string Description => "Flickering fire effect";

    public FireEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "baseColor",
            "Base Color",
            Colors.Orange
        ));

        _parameters.Add(new ColorParameter(
            "tipColor",
            "Flame Tip Color",
            Colors.Yellow
        ));

        _parameters.Add(new RangeParameter(
            "intensity",
            "Intensity",
            EffectParameterType.Intensity,
            defaultValue: 70,
            minValue: 0,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "speed",
            "Flicker Speed",
            EffectParameterType.Speed,
            defaultValue: 50,
            minValue: 1,
            maxValue: 100
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _heatMap = new double[MaxRows, MaxCols];
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null || _heatMap == null) return;

        var baseColor = GetParameter<ColorParameter>("baseColor")?.ColorValue ?? Colors.Orange;
        var tipColor = GetParameter<ColorParameter>("tipColor")?.ColorValue ?? Colors.Yellow;
        var intensity = GetParameter<RangeParameter>("intensity")?.NumericValue ?? 70;
        var speed = GetParameter<RangeParameter>("speed")?.NumericValue ?? 50;

        var speedMultiplier = speed / 50.0;

        // Generate heat at bottom
        for (int col = 0; col < MaxCols; col++)
        {
            _heatMap[MaxRows - 1, col] = _random.NextDouble() * intensity / 100.0;
        }

        // Propagate heat upward
        for (int row = 0; row < MaxRows - 1; row++)
        {
            for (int col = 0; col < MaxCols; col++)
            {
                var heat = _heatMap[row + 1, col];
                
                if (col > 0)
                    heat += _heatMap[row + 1, col - 1] * 0.1;
                if (col < MaxCols - 1)
                    heat += _heatMap[row + 1, col + 1] * 0.1;

                heat = heat * 0.85 * speedMultiplier;
                _heatMap[row, col] = Math.Clamp(heat + (_random.NextDouble() - 0.5) * 0.1, 0, 1);
            }
        }

        // Apply colors based on heat
        for (int row = 0; row < MaxRows; row++)
        {
            for (int col = 0; col < MaxCols; col++)
            {
                var heat = _heatMap[row, col];
                var color = InterpolateColor(baseColor, tipColor, heat);

                _colorBuffer[row, col] = new KeyColour(color.R, color.G, color.B);
            }
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    private (byte R, byte G, byte B) InterpolateColor(Color start, Color end, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return (
            (byte)(start.R + (end.R - start.R) * t),
            (byte)(start.G + (end.G - start.G) * t),
            (byte)(start.B + (end.B - start.B) * t)
        );
    }

    public override void Cleanup()
    {
        _heatMap = null;
    }
}
