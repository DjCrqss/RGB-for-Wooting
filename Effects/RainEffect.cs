using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;

namespace WootingRGB.Effects;

public class RainEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly Random _random = new();
    private readonly List<Raindrop> _raindrops = new();

    public override string Name => "Rain";
    public override string Description => "Raindrops falling down the keyboard";

    private class Raindrop
    {
        public int Column { get; set; }
        public double Row { get; set; }
        public Color Color { get; set; }
        public double Speed { get; set; }
    }

    public RainEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "color",
            "Rain Color",
            Colors.Blue
        ));

        _parameters.Add(new RangeParameter(
            "speed",
            "Fall Speed",
            EffectParameterType.Speed,
            defaultValue: 50,
            minValue: 1,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "density",
            "Rain Density",
            EffectParameterType.Intensity,
            defaultValue: 30,
            minValue: 1,
            maxValue: 100
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _raindrops.Clear();
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var colorParam = GetParameter<ColorParameter>("color");
        var speed = GetParameter<RangeParameter>("speed")?.NumericValue ?? 50;
        var density = GetParameter<RangeParameter>("density")?.NumericValue ?? 30;

        if (colorParam == null) return;

        // Spawn new raindrops
        if (_random.Next(100) < density)
        {
            _raindrops.Add(new Raindrop
            {
                Column = _random.Next(MaxCols),
                Row = 0,
                Color = colorParam.ColorValue,
                Speed = speed / 20.0
            });
        }

        // Clear buffer
        ClearBuffer();

        // Update and draw raindrops
        for (int i = _raindrops.Count - 1; i >= 0; i--)
        {
            var drop = _raindrops[i];
            drop.Row += drop.Speed;

            if (drop.Row >= MaxRows)
            {
                _raindrops.RemoveAt(i);
                continue;
            }

            var row = (int)drop.Row;
            var intensity = 1.0 - (drop.Row - row);

            _colorBuffer[row, drop.Column] = new KeyColour(
                (byte)(drop.Color.R * intensity),
                (byte)(drop.Color.G * intensity),
                (byte)(drop.Color.B * intensity)
            );
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    public override void Cleanup()
    {
        _raindrops.Clear();
    }
}
