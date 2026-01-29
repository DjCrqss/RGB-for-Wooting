using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;

namespace WootingRGB.Effects;

public class StaticColorEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;

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

        if (colorParam == null || _colorBuffer == null) return;

        var color = colorParam.ColorValue;
        var brightnessMultiplier = brightness / 100.0;

        var r = (byte)(color.R * brightnessMultiplier);
        var g = (byte)(color.G * brightnessMultiplier);
        var b = (byte)(color.B * brightnessMultiplier);

        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                SetPixel(row, col, r, g, b);
            }
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }
}
