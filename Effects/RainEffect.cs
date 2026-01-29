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
    private readonly Dictionary<(int row, int col), (Color color, double intensity)> _trailData = new();

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
            defaultValue: 10,
            minValue: 1,
            maxValue: 20
        ));

        _parameters.Add(new RangeParameter(
            "density",
            "Rain Density",
            EffectParameterType.Intensity,
            defaultValue: 30,
            minValue: 1,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "trailLength",
            "Trail Length",
            EffectParameterType.Size,
            defaultValue: 50,
            minValue: 10,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "colorVariation",
            "Color Variation",
            EffectParameterType.Intensity,
            defaultValue: 20,
            minValue: 0,
            maxValue: 100
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _raindrops.Clear();
        _trailData.Clear();
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var colorParam = GetParameter<ColorParameter>("color");
        var speed = GetParameter<RangeParameter>("speed")?.NumericValue ?? 10;
        var density = GetParameter<RangeParameter>("density")?.NumericValue ?? 30;
        var trailLength = GetParameter<RangeParameter>("trailLength")?.NumericValue ?? 50;
        var colorVariation = GetParameter<RangeParameter>("colorVariation")?.NumericValue ?? 20;

        if (colorParam == null) return;

        var baseColor = colorParam.ColorValue;

        // Better scaling: use square root to make the adjustment more balanced
        // This makes slow speeds less aggressive and fast speeds more compensated
        var speedNormalized = speed / 20.0; // 0.05 to 1.0
        var speedFactor = Math.Sqrt(1.0 / speedNormalized); // Square root gives smoother scaling
        var adjustedDensity = (density / 100.0) * speedFactor * 0.15; // Convert to probability per frame

        // Spawn new raindrops
        if (_random.NextDouble() < adjustedDensity)
        {
            _raindrops.Add(new Raindrop
            {
                Column = _random.Next(_keyboardService.MaxColumns),
                Row = 0,
                Color = VaryColor(baseColor, colorVariation),
                Speed = speed / 20.0
            });
        }

        // Fade existing trails
        var fadeRate = 0.05 * (100.0 / trailLength);
        var trailsToRemove = new List<(int, int)>();
        
        foreach (var kvp in _trailData.ToList())
        {
            var (color, intensity) = kvp.Value;
            var newIntensity = intensity - fadeRate;
            if (newIntensity <= 0)
            {
                trailsToRemove.Add(kvp.Key);
            }
            else
            {
                _trailData[kvp.Key] = (color, newIntensity);
            }
        }

        foreach (var key in trailsToRemove)
        {
            _trailData.Remove(key);
        }

        // Update and draw raindrops
        for (int i = _raindrops.Count - 1; i >= 0; i--)
        {
            var drop = _raindrops[i];
            
            // Add current position to trail with full intensity and drop's color
            var currentRow = (int)drop.Row;
            if (currentRow >= 0 && currentRow < _keyboardService.MaxRows)
            {
                _trailData[(currentRow, drop.Column)] = (drop.Color, 1.0);
            }

            // Move raindrop
            drop.Row += drop.Speed;

            // Remove if off screen
            if (drop.Row >= _keyboardService.MaxRows)
            {
                _raindrops.RemoveAt(i);
            }
        }

        // Clear buffer
        ClearBuffer();

        // Draw all trails with their specific colors
        foreach (var kvp in _trailData)
        {
            var (row, col) = kvp.Key;
            var (color, intensity) = kvp.Value;

            _colorBuffer[row, col] = new KeyColour(
                (byte)(color.R * intensity),
                (byte)(color.G * intensity),
                (byte)(color.B * intensity)
            );
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    private Color VaryColor(Color baseColor, double variation)
    {
        if (variation <= 0) return baseColor;

        // Convert RGB to HSV
        var (h, s, v) = RgbToHsv(baseColor);

        // Vary the hue - at 100% variation, full 360 degree range for rainbow rain
        var variationAmount = variation / 100.0;
        var hueShift = (_random.NextDouble() - 0.5) * variationAmount * 720; // +/- 360 degrees at max variation
        h = (h + hueShift + 360) % 360; // Keep in 0-360 range

        // Convert back to RGB
        return HsvToRgb(h, s, v);
    }

    private (double h, double s, double v) RgbToHsv(Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double h = 0;
        if (delta != 0)
        {
            if (max == r)
                h = 60 * (((g - b) / delta) % 6);
            else if (max == g)
                h = 60 * (((b - r) / delta) + 2);
            else
                h = 60 * (((r - g) / delta) + 4);
        }
        if (h < 0) h += 360;

        double s = max == 0 ? 0 : delta / max;
        double v = max;

        return (h, s, v);
    }

    private Color HsvToRgb(double h, double s, double v)
    {
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = v - c;

        double r = 0, g = 0, b = 0;

        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255)
        );
    }

    public override void Cleanup()
    {
        _raindrops.Clear();
        _trailData.Clear();
    }
}
