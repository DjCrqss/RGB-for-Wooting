using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;

namespace WootingRGB.Effects;

public class SparklesEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly Random _random = new();
    private readonly List<Sparkle> _sparkles = new();

    public override string Name => "Sparkles";
    public override string Description => "Random sparkling lights";

    private class Sparkle
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public Color Color { get; set; }
        public double Lifetime { get; set; }
        public double MaxLifetime { get; set; }
    }

    public SparklesEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "color1",
            "Sparkle Color 1",
            Colors.White
        ));

        _parameters.Add(new ColorParameter(
            "color2",
            "Sparkle Color 2",
            Colors.Cyan
        ));

        _parameters.Add(new RangeParameter(
            "density",
            "Sparkle Density",
            EffectParameterType.Intensity,
            defaultValue: 20,
            minValue: 1,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "speed",
            "Fade Speed",
            EffectParameterType.Speed,
            defaultValue: 50,
            minValue: 1,
            maxValue: 100
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _sparkles.Clear();
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var color1 = GetParameter<ColorParameter>("color1")?.ColorValue ?? Colors.White;
        var color2 = GetParameter<ColorParameter>("color2")?.ColorValue ?? Colors.Cyan;
        var density = GetParameter<RangeParameter>("density")?.NumericValue ?? 20;
        var speed = GetParameter<RangeParameter>("speed")?.NumericValue ?? 50;

        // Spawn new sparkles
        if (_random.Next(100) < density)
        {
            _sparkles.Add(new Sparkle
            {
                Row = _random.Next(_keyboardService.MaxRows),
                Col = _random.Next(_keyboardService.MaxColumns),
                Color = _random.Next(2) == 0 ? color1 : color2,
                Lifetime = 0,
                MaxLifetime = 0.5 + _random.NextDouble() * 0.5
            });
        }

        // Clear buffer
        ClearBuffer();

        // Update and draw sparkles
        var deltaTime = 0.016 * (speed / 50.0); // Assuming ~60fps
        for (int i = _sparkles.Count - 1; i >= 0; i--)
        {
            var sparkle = _sparkles[i];
            sparkle.Lifetime += deltaTime;

            if (sparkle.Lifetime >= sparkle.MaxLifetime)
            {
                _sparkles.RemoveAt(i);
                continue;
            }

            var progress = sparkle.Lifetime / sparkle.MaxLifetime;
            var brightness = Math.Sin(progress * Math.PI);

            _colorBuffer[sparkle.Row, sparkle.Col] = new KeyColour(
                (byte)(sparkle.Color.R * brightness),
                (byte)(sparkle.Color.G * brightness),
                (byte)(sparkle.Color.B * brightness)
            );
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    public override void Cleanup()
    {
        _sparkles.Clear();
    }
}
