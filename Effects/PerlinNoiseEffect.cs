using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;

namespace WootingRGB.Effects;

public class PerlinNoiseEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly PerlinNoise _perlinNoise;
    private double _time = 0;
    private Dictionary<(int row, int col), double> _keyPressDepths = new();

    public override string Name => "Perlin Noise";
    public override string Description => "Flowing Perlin noise with analog key press interaction";

    public PerlinNoiseEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
        _perlinNoise = new PerlinNoise();
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "color1",
            "Noise Color 1",
            Color.FromRgb(0x8A, 0x2B, 0xE2) // #8A2BE2 - Blue Violet
        ));

        _parameters.Add(new ColorParameter(
            "color2",
            "Noise Color 2",
            Color.FromRgb(0xFF, 0xC8, 0x00) // #FFC800 - Orange/Gold
        ));

        _parameters.Add(new ColorParameter(
            "pressColor",
            "Press Color",
            Color.FromRgb(0x00, 0xFF, 0x5C) // #00FF5C - Bright Green
        ));

        _parameters.Add(new RangeParameter(
            "speed",
            "Flow Speed",
            EffectParameterType.Speed,
            defaultValue: 25,
            minValue: 1,
            maxValue: 50
        ));

        _parameters.Add(new RangeParameter(
            "scale",
            "Noise Scale",
            EffectParameterType.Size,
            defaultValue: 15,
            minValue: 5,
            maxValue: 50
        ));

        _parameters.Add(new RangeParameter(
            "pressDepth",
            "Press Depth",
            EffectParameterType.Intensity,
            defaultValue: 50,
            minValue: 10,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "pressRadius",
            "Press Radius",
            EffectParameterType.Size,
            defaultValue: 25,
            minValue: 10,
            maxValue: 50
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _time = 0;
        _keyPressDepths.Clear();
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var color1 = GetParameter<ColorParameter>("color1")?.ColorValue ?? Colors.Purple;
        var color2 = GetParameter<ColorParameter>("color2")?.ColorValue ?? Colors.Blue;
        var pressColor = GetParameter<ColorParameter>("pressColor")?.ColorValue ?? Colors.HotPink;
        var speed = GetParameter<RangeParameter>("speed")?.NumericValue ?? 10;
        var scale = GetParameter<RangeParameter>("scale")?.NumericValue ?? 15;
        var pressDepth = GetParameter<RangeParameter>("pressDepth")?.NumericValue ?? 50;
        var pressRadius = GetParameter<RangeParameter>("pressRadius")?.NumericValue ?? 25;

        _time += speed * 0.001; // Much slower speed multiplier (was 0.01)

        // Update key press depths with fade out
        var fadeRate = 0.05;
        var keysToRemove = new List<(int, int)>();
        
        foreach (var kvp in _keyPressDepths.ToList())
        {
            var newDepth = kvp.Value - fadeRate;
            if (newDepth <= 0)
            {
                keysToRemove.Add(kvp.Key);
            }
            else
            {
                _keyPressDepths[kvp.Key] = newDepth;
            }
        }

        foreach (var key in keysToRemove)
        {
            _keyPressDepths.Remove(key);
        }

        // Update pressed keys from analog input
        foreach (var pressedKey in keyboardState.PressedKeys)
        {
            // Convert keycode to row/col (simplified - you may need proper mapping)
            var keyCode = pressedKey.Key;
            var pressure = Math.Clamp(pressedKey.Value, 0, 1);
            
            // Try to find key position (this is a simplified approach)
            // In a real implementation, you'd have a keycode-to-position lookup table
            var row = (keyCode / 100) % _keyboardService.MaxRows;
            var col = keyCode % _keyboardService.MaxColumns;
            
            if (row >= 0 && row < _keyboardService.MaxRows && 
                col >= 0 && col < _keyboardService.MaxColumns)
            {
                _keyPressDepths[(row, col)] = pressure;
            }
        }

        // Render the effect
        var scaleValue = scale / 100.0;
        var radiusValue = pressRadius / 100.0 * 5; // Scale to reasonable radius

        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                // Generate base Perlin noise value with 3D noise for moving blobs
                // Use time as the Z coordinate to create evolving patterns
                var noiseX = col * scaleValue;
                var noiseY = row * scaleValue;
                var noiseZ = _time;
                var noiseValue = _perlinNoise.Noise(noiseX, noiseY, noiseZ);
                
                // Normalize to 0-0.66 range (Perlin noise is typically -1 to 1)
                // Add contrast enhancement to make colors more vibrant
                noiseValue = (noiseValue + 1) * 0.5; // Normalize to 0-1
                
                // Apply contrast curve to make colors pop more
                noiseValue = Math.Pow(noiseValue, 0.7); // Slight power curve for better contrast
                
                noiseValue *= 0.66; // Scale to 0-0.66 range

                // Calculate influence from pressed keys
                double pressHeight = 0;

                foreach (var pressedKeyPos in _keyPressDepths)
                {
                    var (pressRow, pressCol) = pressedKeyPos.Key;
                    var pressure = pressedKeyPos.Value;

                    // Calculate distance from current position to pressed key
                    var distance = Math.Sqrt(
                        Math.Pow(row - pressRow, 2) + 
                        Math.Pow(col - pressCol, 2)
                    );

                    // Apply radius-based falloff
                    if (distance < radiusValue)
                    {
                        var falloff = 1.0 - (distance / radiusValue);
                        var influence = falloff * pressure * (pressDepth / 100.0);
                        
                        // Accumulate the maximum influence
                        pressHeight = Math.Max(pressHeight, influence);
                    }
                }

                // Calculate final height: base noise (0-0.66) + press height (0-0.34)
                // This ensures pressed keys can push height up to 1.0
                var totalHeight = noiseValue + (pressHeight * 0.34);
                totalHeight = Math.Clamp(totalHeight, 0, 1);

                // Map height to colors
                Color finalColor;
                if (totalHeight <= 0.66)
                {
                    // Interpolate between color1 and color2 for the noise range (0-0.66)
                    var noiseT = totalHeight / 0.66;
                    finalColor = InterpolateColor(color1, color2, noiseT);
                }
                else
                {
                    // Interpolate from color2 to pressColor for the press range (0.66-1.0)
                    var pressT = (totalHeight - 0.66) / 0.34;
                    finalColor = InterpolateColor(color2, pressColor, pressT);
                }

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

    private Color InterpolateColor(Color start, Color end, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromRgb(
            (byte)(start.R + (end.R - start.R) * t),
            (byte)(start.G + (end.G - start.G) * t),
            (byte)(start.B + (end.B - start.B) * t)
        );
    }

    public override void Cleanup()
    {
        _keyPressDepths.Clear();
    }

    // Simple Perlin Noise implementation
    private class PerlinNoise
    {
        private const int PermutationSize = 256;
        private readonly int[] _permutation;

        public PerlinNoise(int seed = 0)
        {
            _permutation = new int[PermutationSize * 2];
            var random = new Random(seed);
            
            var p = new int[PermutationSize];
            for (int i = 0; i < PermutationSize; i++)
                p[i] = i;

            // Shuffle
            for (int i = 0; i < PermutationSize; i++)
            {
                int j = random.Next(PermutationSize);
                (p[i], p[j]) = (p[j], p[i]);
            }

            // Duplicate for easy wrapping
            for (int i = 0; i < PermutationSize * 2; i++)
                _permutation[i] = p[i % PermutationSize];
        }

        public double Noise(double x, double y, double z)
        {
            // Find unit cube containing point
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;
            int Z = (int)Math.Floor(z) & 255;

            // Find relative position in cube
            x -= Math.Floor(x);
            y -= Math.Floor(y);
            z -= Math.Floor(z);

            // Compute fade curves
            double u = Fade(x);
            double v = Fade(y);
            double w = Fade(z);

            // Hash coordinates of cube corners
            int A = _permutation[X] + Y;
            int AA = _permutation[A] + Z;
            int AB = _permutation[A + 1] + Z;
            int B = _permutation[X + 1] + Y;
            int BA = _permutation[B] + Z;
            int BB = _permutation[B + 1] + Z;

            // Blend results from 8 corners
            return Lerp(w,
                Lerp(v,
                    Lerp(u,
                        Grad(_permutation[AA], x, y, z),
                        Grad(_permutation[BA], x - 1, y, z)),
                    Lerp(u,
                        Grad(_permutation[AB], x, y - 1, z),
                        Grad(_permutation[BB], x - 1, y - 1, z))),
                Lerp(v,
                    Lerp(u,
                        Grad(_permutation[AA + 1], x, y, z - 1),
                        Grad(_permutation[BA + 1], x - 1, y, z - 1)),
                    Lerp(u,
                        Grad(_permutation[AB + 1], x, y - 1, z - 1),
                        Grad(_permutation[BB + 1], x - 1, y - 1, z - 1))));
        }

        private double Fade(double t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private double Lerp(double t, double a, double b)
        {
            return a + t * (b - a);
        }

        private double Grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;
            double u = h < 8 ? x : y;
            double v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}
