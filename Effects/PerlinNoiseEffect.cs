using System.Windows.Media;
using System.Collections.Concurrent;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;
using MediaColor = System.Windows.Media.Color;

namespace WootingRGB.Effects;

public class PerlinNoiseEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly PerlinNoise _perlinNoise;
    private double _time = 0;
    private readonly ConcurrentDictionary<(int row, int col), double> _keyPressDepths = new();

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
            MediaColor.FromRgb(0x8A, 0x2B, 0xE2) // #8A2BE2 - Blue Violet
        ));

        _parameters.Add(new ColorParameter(
            "color2",
            "Noise Color 2",
            MediaColor.FromRgb(0xFF, 0xC8, 0x00) // #FFC800 - Orange/Gold
        ));

        _parameters.Add(new ColorParameter(
            "pressColor",
            "Press Color",
            MediaColor.FromRgb(0x00, 0xFF, 0x5C) // #00FF5C - Bright Green
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
            defaultValue: 50,
            minValue: 5,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "pressDepth",
            "Press Depth",
            EffectParameterType.Intensity,
            defaultValue: 200,
            minValue: 10,
            maxValue: 200
        ));

        _parameters.Add(new RangeParameter(
            "pressRadius",
            "Press Radius",
            EffectParameterType.Size,
            defaultValue: 64,
            minValue: 10,
            maxValue: 150
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
        var keysToUpdate = new List<((int, int) key, double newValue)>();
        var keysToRemove = new List<(int, int)>();
        
        foreach (var kvp in _keyPressDepths)
        {
            var newDepth = kvp.Value - fadeRate;
            if (newDepth <= 0)
            {
                keysToRemove.Add(kvp.Key);
            }
            else
            {
                keysToUpdate.Add((kvp.Key, newDepth));
            }
        }

        // Apply updates
        foreach (var (key, value) in keysToUpdate)
        {
            _keyPressDepths[key] = value;
        }
        
        foreach (var key in keysToRemove)
        {
            _keyPressDepths.TryRemove(key, out _);
        }

        // Update pressed keys from analog input
        foreach (var pressedKey in keyboardState.PressedKeys)
        {
            var keyCode = pressedKey.Key;
            var pressure = Math.Clamp(pressedKey.Value, 0, 1);
            
            // Use the proper HID code to position mapping from the helper class
            if (WootingAnalogLedMapping.HidToPosition.TryGetValue(keyCode, out var position))
            {
                var (row, col) = position;
                
                if (row >= 0 && row < _keyboardService.MaxRows && 
                    col >= 0 && col < _keyboardService.MaxColumns)
                {
                    _keyPressDepths[position] = pressure;
                }
            }
        }

        // Render the effect
        var scaleValue = scale / 100.0;
        var radiusValue = pressRadius / 100.0 * 5;

        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                var noiseX = col * scaleValue;
                var noiseY = row * scaleValue;
                var noiseZ = _time;
                var noiseValue = _perlinNoise.Noise(noiseX, noiseY, noiseZ);
                
                noiseValue = (noiseValue + 1) * 0.5; // normalize to 0-1
                noiseValue = Math.Pow(noiseValue, 0.7); // Slight power curve for better contrast
                noiseValue *= 0.66; // Scale to 0-0.66 range (other third is for pressed keys)

                double pressHeight = 0;
                foreach (var pressedKeyPos in _keyPressDepths)
                {
                    var (pressRow, pressCol) = pressedKeyPos.Key;
                    var pressure = pressedKeyPos.Value;

                    // calc dstance from current position to pressed key
                    var distance = Math.Sqrt(
                        Math.Pow(row - pressRow, 2) + 
                        Math.Pow(col - pressCol, 2)
                    );

                    // radius-based falloff
                    if (distance < radiusValue)
                    {
                        var falloff = 1.0 - (distance / radiusValue);
                        var influence = falloff * pressure * (pressDepth / 100.0);
                        
                        // Accumulate the maximum influence
                        pressHeight = Math.Max(pressHeight, influence);
                    }
                }

                // calculate final height: base noise (0-0.66) + press height (0-0.34)
                // This ensures pressed keys can push height up to 1.0
                var totalHeight = noiseValue + (pressHeight * 0.34);
                totalHeight = Math.Clamp(totalHeight, 0, 1);

                // Map height to colors
                MediaColor finalColor;
                if (totalHeight <= 0.66)
                {
                    // Interpolate between color1 and color2 for the noise range (0-0.66)
                    var noiseT = totalHeight / 0.66;
                    finalColor = EffectUtilities.LerpColor(color1, color2, noiseT);
                }
                else
                {
                    // Interpolate from color2 to pressColor for the press range (0.66-1.0)
                    var pressT = (totalHeight - 0.66) / 0.34;
                    finalColor = EffectUtilities.LerpColor(color2, pressColor, pressT);
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

    public override void Cleanup()
    {
        _keyPressDepths.Clear();
    }

    
    private class PerlinNoise
    {
        private const int PermutationSize = 256;
        private readonly byte[] _permutation;

        public PerlinNoise(int seed = 0)
        {
            _permutation = new byte[PermutationSize];
            var random = new Random(seed);
            
            for (int i = 0; i < PermutationSize; i++)
                _permutation[i] = (byte)i;

            for (int i = PermutationSize - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (_permutation[i], _permutation[j]) = (_permutation[j], _permutation[i]);
            }
        }

        public double Noise(double x, double y, double z)
        {
            int X = ((int)x) & 255;
            int Y = ((int)y) & 255;
            int Z = ((int)z) & 255;

            x -= (int)x;
            y -= (int)y;
            z -= (int)z;

            // fade curves
            double u = x * x * x * (x * (x * 6 - 15) + 10);
            double v = y * y * y * (y * (y * 6 - 15) + 10);
            double w = z * z * z * (z * (z * 6 - 15) + 10);

            int A = _permutation[X] + Y;
            int AA = _permutation[A & 255] + Z;
            int AB = _permutation[(A + 1) & 255] + Z;
            int B = _permutation[(X + 1) & 255] + Y;
            int BA = _permutation[B & 255] + Z;
            int BB = _permutation[(B + 1) & 255] + Z;

            // trilinear interpolation
            double x1 = x - 1, y1 = y - 1, z1 = z - 1;
            
            double c000 = Grad(_permutation[AA & 255], x, y, z);
            double c100 = Grad(_permutation[BA & 255], x1, y, z);
            double c010 = Grad(_permutation[AB & 255], x, y1, z);
            double c110 = Grad(_permutation[BB & 255], x1, y1, z);
            double c001 = Grad(_permutation[(AA + 1) & 255], x, y, z1);
            double c101 = Grad(_permutation[(BA + 1) & 255], x1, y, z1);
            double c011 = Grad(_permutation[(AB + 1) & 255], x, y1, z1);
            double c111 = Grad(_permutation[(BB + 1) & 255], x1, y1, z1);

            // x
            double nx00 = c000 + u * (c100 - c000);
            double nx01 = c001 + u * (c101 - c001);
            double nx10 = c010 + u * (c110 - c010);
            double nx11 = c011 + u * (c111 - c011);

            // y
            double nxy0 = nx00 + v * (nx10 - nx00);
            double nxy1 = nx01 + v * (nx11 - nx01);

            // z
            return nxy0 + w * (nxy1 - nxy0);
        }

        // gradient function 
        private static double Grad(int hash, double x, double y, double z)
        {
            switch (hash & 15)
            {
                case 0: return x + y;
                case 1: return -x + y;
                case 2: return x - y;
                case 3: return -x - y;
                case 4: return x + z;
                case 5: return -x + z;
                case 6: return x - z;
                case 7: return -x - z;
                case 8: return y + z;
                case 9: return -y + z;
                case 10: return y - z;
                case 11: return -y - z;
                case 12: return y + x;
                case 13: return -y + z;
                case 14: return y - x;
                case 15: return -y - z;
                default: return 0;
            }
        }
    }
}
