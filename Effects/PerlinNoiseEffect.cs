using System.Windows.Media;
using System.Collections.Concurrent;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;
using RGB.NET.Core;
using MediaColor = System.Windows.Media.Color;

namespace WootingRGB.Effects;

public class PerlinNoiseEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly PerlinNoise _perlinNoise;
    private double _time = 0;
    private readonly ConcurrentDictionary<(int row, int col), double> _keyPressDepths = new();
    private static readonly Dictionary<short, (int row, int col)> _hidToPosition = InitializeHidToPositionMap();

    public override string Name => "Perlin Noise";
    public override string Description => "Flowing Perlin noise with analog key press interaction";

    public PerlinNoiseEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
        _perlinNoise = new PerlinNoise();
    }

    private static Dictionary<short, (int row, int col)> InitializeHidToPositionMap()
    {
        var map = new Dictionary<short, (int row, int col)>();
        
        foreach (var kvp in WootingAnalogLedMapping.HidCodesReversed)
        {
            var hidCode = kvp.Key;
            var ledId = kvp.Value;
            var position = GetLedIdPosition(ledId);
            if (position.HasValue)
            {
                map[hidCode] = position.Value;
            }
        }
        
        return map;
    }

    private static (int row, int col)? GetLedIdPosition(LedId ledId)
    {
        // Map LedId to keyboard row/col positions (0-indexed)
        // Based on standard keyboard layout
        return ledId switch
        {
            // Row 0 - F keys and extras
            LedId.Keyboard_Escape => (0, 0),
            LedId.Keyboard_F1 => (0, 2),
            LedId.Keyboard_F2 => (0, 3),
            LedId.Keyboard_F3 => (0, 4),
            LedId.Keyboard_F4 => (0, 5),
            LedId.Keyboard_F5 => (0, 6),
            LedId.Keyboard_F6 => (0, 7),
            LedId.Keyboard_F7 => (0, 8),
            LedId.Keyboard_F8 => (0, 9),
            LedId.Keyboard_F9 => (0, 10),
            LedId.Keyboard_F10 => (0, 11),
            LedId.Keyboard_F11 => (0, 12),
            LedId.Keyboard_F12 => (0, 13),
            LedId.Keyboard_PrintScreen => (0, 14),
            LedId.Keyboard_ScrollLock => (0, 15),
            LedId.Keyboard_PauseBreak => (0, 16),

            // Row 1 - Number row
            LedId.Keyboard_GraveAccentAndTilde => (1, 0),
            LedId.Keyboard_1 => (1, 1),
            LedId.Keyboard_2 => (1, 2),
            LedId.Keyboard_3 => (1, 3),
            LedId.Keyboard_4 => (1, 4),
            LedId.Keyboard_5 => (1, 5),
            LedId.Keyboard_6 => (1, 6),
            LedId.Keyboard_7 => (1, 7),
            LedId.Keyboard_8 => (1, 8),
            LedId.Keyboard_9 => (1, 9),
            LedId.Keyboard_0 => (1, 10),
            LedId.Keyboard_MinusAndUnderscore => (1, 11),
            LedId.Keyboard_EqualsAndPlus => (1, 12),
            LedId.Keyboard_Backspace => (1, 13),
            LedId.Keyboard_Insert => (1, 14),
            LedId.Keyboard_Home => (1, 15),
            LedId.Keyboard_PageUp => (1, 16),

            // Row 2 - QWERTY row
            LedId.Keyboard_Tab => (2, 0),
            LedId.Keyboard_Q => (2, 1),
            LedId.Keyboard_W => (2, 2),
            LedId.Keyboard_E => (2, 3),
            LedId.Keyboard_R => (2, 4),
            LedId.Keyboard_T => (2, 5),
            LedId.Keyboard_Y => (2, 6),
            LedId.Keyboard_U => (2, 7),
            LedId.Keyboard_I => (2, 8),
            LedId.Keyboard_O => (2, 9),
            LedId.Keyboard_P => (2, 10),
            LedId.Keyboard_BracketLeft => (2, 11),
            LedId.Keyboard_BracketRight => (2, 12),
            LedId.Keyboard_Backslash => (2, 13),
            LedId.Keyboard_Delete => (2, 14),
            LedId.Keyboard_End => (2, 15),
            LedId.Keyboard_PageDown => (2, 16),

            // Row 3 - ASDF row
            LedId.Keyboard_CapsLock => (3, 0),
            LedId.Keyboard_A => (3, 1),
            LedId.Keyboard_S => (3, 2),
            LedId.Keyboard_D => (3, 3),
            LedId.Keyboard_F => (3, 4),
            LedId.Keyboard_G => (3, 5),
            LedId.Keyboard_H => (3, 6),
            LedId.Keyboard_J => (3, 7),
            LedId.Keyboard_K => (3, 8),
            LedId.Keyboard_L => (3, 9),
            LedId.Keyboard_SemicolonAndColon => (3, 10),
            LedId.Keyboard_ApostropheAndDoubleQuote => (3, 11),
            LedId.Keyboard_Enter => (3, 12),

            // Row 4 - ZXCV row
            LedId.Keyboard_LeftShift => (4, 0),
            LedId.Keyboard_Z => (4, 1),
            LedId.Keyboard_X => (4, 2),
            LedId.Keyboard_C => (4, 3),
            LedId.Keyboard_V => (4, 4),
            LedId.Keyboard_B => (4, 5),
            LedId.Keyboard_N => (4, 6),
            LedId.Keyboard_M => (4, 7),
            LedId.Keyboard_CommaAndLessThan => (4, 8),
            LedId.Keyboard_PeriodAndBiggerThan => (4, 9),
            LedId.Keyboard_SlashAndQuestionMark => (4, 10),
            LedId.Keyboard_RightShift => (4, 11),
            LedId.Keyboard_ArrowUp => (4, 14),

            // Row 5 - Bottom row
            LedId.Keyboard_LeftCtrl => (5, 0),
            LedId.Keyboard_LeftGui => (5, 1),
            LedId.Keyboard_LeftAlt => (5, 2),
            LedId.Keyboard_Space => (5, 5),
            LedId.Keyboard_RightAlt => (5, 9),
            LedId.Keyboard_Function => (5, 10),
            LedId.Keyboard_Application => (5, 11),
            LedId.Keyboard_RightCtrl => (5, 12),
            LedId.Keyboard_ArrowLeft => (5, 13),
            LedId.Keyboard_ArrowDown => (5, 14),
            LedId.Keyboard_ArrowRight => (5, 15),

            _ => null
        };
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
            
            // Use the proper HID code to position mapping
            if (_hidToPosition.TryGetValue(keyCode, out var position))
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
        var radiusValue = pressRadius / 100.0 * 5; // Scale to reasonable radius

        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                var noiseX = col * scaleValue;
                var noiseY = row * scaleValue;
                var noiseZ = _time;
                var noiseValue = _perlinNoise.Noise(noiseX, noiseY, noiseZ);
                
                // Normalize to 0-0.66 range (Perlin noise is typically -1 to 1)
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
                MediaColor finalColor;
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

    private MediaColor InterpolateColor(MediaColor start, MediaColor end, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return MediaColor.FromRgb(
            (byte)(start.R + (end.R - start.R) * t),
            (byte)(start.G + (end.G - start.G) * t),
            (byte)(start.B + (end.B - start.B) * t)
        );
    }

    public override void Cleanup()
    {
        _keyPressDepths.Clear();
    }

    
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

            for (int i = 0; i < PermutationSize; i++)
            {
                int j = random.Next(PermutationSize);
                (p[i], p[j]) = (p[j], p[i]);
            }

            for (int i = 0; i < PermutationSize * 2; i++)
                _permutation[i] = p[i % PermutationSize];
        }

        public double Noise(double x, double y, double z)
        {
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;
            int Z = (int)Math.Floor(z) & 255;

            x -= Math.Floor(x);
            y -= Math.Floor(y);
            z -= Math.Floor(z);

            double u = Fade(x);
            double v = Fade(y);
            double w = Fade(z);

            int A = _permutation[X] + Y;
            int AA = _permutation[A] + Z;
            int AB = _permutation[A + 1] + Z;
            int B = _permutation[X + 1] + Y;
            int BA = _permutation[B] + Z;
            int BB = _permutation[B + 1] + Z;

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
