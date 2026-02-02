using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;
using MediaColor = System.Windows.Media.Color;

namespace WootingRGB.Effects;

public class NetherPortalEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private double _time = 0;
    private readonly Random _random = new();
    private double[,]? _noiseOffsets;

    public override string Name => "Nether Portal";
    public override string Description => "Swirling portal effect like Minecraft's Nether Portal";

    public NetherPortalEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "darkColor",
            "Dark Color",
            MediaColor.FromRgb(0x1A, 0x00, 0x33) // Deep purple
        ));

        _parameters.Add(new ColorParameter(
            "brightColor",
            "Bright Color",
            MediaColor.FromRgb(0xBB, 0x00, 0xFF) // Bright magenta
        ));

        _parameters.Add(new RangeParameter(
            "speed",
            "Animation Speed",
            EffectParameterType.Speed,
            defaultValue: 30,
            minValue: 1,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "swirl",
            "Swirl Intensity",
            EffectParameterType.Intensity,
            defaultValue: 60,
            minValue: 10,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "turbulence",
            "Turbulence",
            EffectParameterType.Intensity,
            defaultValue: 50,
            minValue: 10,
            maxValue: 100
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _time = 0;
        
        // Initialize noise offsets for each pixel for variation
        _noiseOffsets = new double[_keyboardService.MaxRows, _keyboardService.MaxColumns];
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                _noiseOffsets[row, col] = _random.NextDouble() * 100;
            }
        }
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null || _noiseOffsets == null) return;

        var darkColor = GetParameter<ColorParameter>("darkColor")?.ColorValue ?? MediaColor.FromRgb(26, 0, 51);
        var brightColor = GetParameter<ColorParameter>("brightColor")?.ColorValue ?? MediaColor.FromRgb(187, 0, 255);
        var speed = (GetParameter<RangeParameter>("speed")?.NumericValue ?? 30) / 100.0;
        var swirl = (GetParameter<RangeParameter>("swirl")?.NumericValue ?? 60) / 100.0;
        var turbulence = (GetParameter<RangeParameter>("turbulence")?.NumericValue ?? 50) / 100.0;

        _time += speed * 0.1;

        double centerX = _keyboardService.MaxColumns / 2.0;
        double centerY = _keyboardService.MaxRows / 2.0;

        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                // Calculate distance from center
                double dx = col - centerX;
                double dy = row - centerY;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                
                // Calculate angle from center
                double angle = Math.Atan2(dy, dx);

                // Create swirling motion - faster swirl near center
                double swirlAmount = swirl * 3.0 / (1.0 + distance * 0.3);
                double animatedAngle = angle + _time * swirlAmount;

                // Multiple layers of animated noise for portal effect
                double noise1 = Math.Sin(animatedAngle * 3.0 + distance * 0.5 + _time * 2.0);
                double noise2 = Math.Sin(animatedAngle * 5.0 - distance * 0.3 - _time * 1.5);
                double noise3 = Math.Cos(distance * 0.8 + _time * 2.5 + animatedAngle * 2.0);
                
                // Add turbulence using pixel-specific offset
                double turbulenceNoise = Math.Sin(_time * 3.0 + _noiseOffsets[row, col]) * turbulence;
                
                // Combine noise layers
                double combined = (noise1 + noise2 + noise3 + turbulenceNoise) * 0.25;
                
                // Add pulsing brightness from center
                double pulse = Math.Sin(_time * 1.5) * 0.5 + 0.5;
                double centerGlow = Math.Max(0, 1.0 - distance / 8.0) * pulse * 0.3;
                
                combined += centerGlow;
                
                // Normalize to 0-1
                double value = (combined + 1.5) / 3.0;
                value = Math.Clamp(value, 0, 1);
                
                // Apply power curve for more contrast (portal-like appearance)
                value = Math.Pow(value, 1.5);
                
                // Add occasional bright "particles" moving through
                if (_random.NextDouble() < 0.002 * speed)
                {
                    value = Math.Min(1.0, value + 0.5);
                }

                // Interpolate between dark and bright purple
                var finalColor = EffectUtilities.LerpColor(darkColor, brightColor, value);

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

    public override void Cleanup()
    {
        _noiseOffsets = null;
    }
}
