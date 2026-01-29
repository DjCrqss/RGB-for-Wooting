using System.Windows.Media;
using WootingRGB.Core;
using WootingRGB.Services;

namespace WootingRGB.Effects;

public class RainbowEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private const int MaxRows = 6;
    private const int MaxCols = 21;

    public override string Name => "Rainbow Wave";
    public override string Description => "A flowing rainbow effect across the keyboard";

    public RainbowEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new RangeParameter(
            "speed",
            "Wave Speed",
            EffectParameterType.Speed,
            defaultValue: 50,
            minValue: 1,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "intensity",
            "Brightness",
            EffectParameterType.Intensity,
            defaultValue: 100,
            minValue: 0,
            maxValue: 100
        ));
    }

    public override void Update(KeyboardState keyboardState)
    {
        var speed = GetParameter<RangeParameter>("speed")?.NumericValue ?? 50;
        var intensity = GetParameter<RangeParameter>("intensity")?.NumericValue ?? 100;

        var elapsed = (DateTime.Now - _startTime).TotalSeconds;
        var offset = elapsed * (speed / 10.0);

        for (byte row = 0; row < MaxRows; row++)
        {
            for (byte col = 0; col < MaxCols; col++)
            {
                var hue = (col * 15 + row * 5 + offset * 30) % 360;
                var color = HsvToRgb(hue, 1.0, intensity / 100.0);
                
                _keyboardService.SetKeyColor(row, col, color.R, color.G, color.B);
            }
        }

        _keyboardService.UpdateKeyboard();
    }

    private (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
    {
        var c = v * s;
        var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        var m = v - c;

        double r = 0, g = 0, b = 0;

        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return ((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }
}
