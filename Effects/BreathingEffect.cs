using System.Windows.Media;
using WootingRGB.Core;
using WootingRGB.Services;

namespace WootingRGB.Effects;

public class BreathingEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private const int MaxRows = 6;
    private const int MaxCols = 21;

    public override string Name => "Breathing";
    public override string Description => "Smooth breathing color transition";

    public BreathingEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "color1",
            "Color 1",
            Colors.Red
        ));

        _parameters.Add(new ColorParameter(
            "color2",
            "Color 2",
            Colors.Blue
        ));

        _parameters.Add(new RangeParameter(
            "speed",
            "Breathing Speed",
            EffectParameterType.Speed,
            defaultValue: 50,
            minValue: 1,
            maxValue: 100
        ));
    }

    public override void Update(KeyboardState keyboardState)
    {
        var color1 = GetParameter<ColorParameter>("color1")?.ColorValue ?? Colors.Red;
        var color2 = GetParameter<ColorParameter>("color2")?.ColorValue ?? Colors.Blue;
        var speed = GetParameter<RangeParameter>("speed")?.NumericValue ?? 50;

        var elapsed = (DateTime.Now - _startTime).TotalSeconds;
        var cycle = Math.Sin(elapsed * speed / 10.0) * 0.5 + 0.5; // 0 to 1

        var currentColor = InterpolateColor(color1, color2, cycle);

        for (byte row = 0; row < MaxRows; row++)
        {
            for (byte col = 0; col < MaxCols; col++)
            {
                _keyboardService.SetKeyColor(row, col, currentColor.R, currentColor.G, currentColor.B);
            }
        }

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
}
