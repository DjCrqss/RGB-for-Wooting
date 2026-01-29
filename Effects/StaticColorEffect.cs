using System.Windows.Media;
using WootingRGB.Core;
using WootingRGB.Services;

namespace WootingRGB.Effects;

public class StaticColorEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private const int MaxRows = 6;
    private const int MaxCols = 21;

    public override string Name => "Static Color";
    public override string Description => "Solid color across all keys";

    public StaticColorEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "color",
            "Color",
            Colors.Cyan
        ));

        _parameters.Add(new RangeParameter(
            "brightness",
            "Brightness",
            EffectParameterType.Intensity,
            defaultValue: 100,
            minValue: 0,
            maxValue: 100
        ));
    }

    public override void Update(KeyboardState keyboardState)
    {
        var colorParam = GetParameter<ColorParameter>("color");
        var brightness = GetParameter<RangeParameter>("brightness")?.NumericValue ?? 100;

        if (colorParam == null) return;

        var color = colorParam.ColorValue;
        var brightnessMultiplier = brightness / 100.0;

        var r = (byte)(color.R * brightnessMultiplier);
        var g = (byte)(color.G * brightnessMultiplier);
        var b = (byte)(color.B * brightnessMultiplier);

        for (byte row = 0; row < MaxRows; row++)
        {
            for (byte col = 0; col < MaxCols; col++)
            {
                _keyboardService.SetKeyColor(row, col, r, g, b);
            }
        }

        _keyboardService.UpdateKeyboard();
    }
}
