using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;

namespace WootingRGB.Effects;

public class RainEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly Random _random = new();
    private readonly SimplexNoise _noise;
    private readonly List<Raindrop> _raindrops = new();
    private readonly Dictionary<(int row, int col), (Color color, double intensity)> _trailData = new();
    private double _noiseTime = 0;
    private readonly double[] _columnSpawnTimers;

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
        _noise = new SimplexNoise(_random.Next());
        _columnSpawnTimers = new double[21]; // Max keyboard columns
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "backgroundColor",
            "Background Color",
            Color.FromRgb(0x00, 0x00, 0x00) // Black
        ));

        _parameters.Add(new ColorParameter(
            "color",
            "Rain Color",
            Colors.Blue
        ));

        _parameters.Add(new RangeParameter(
            "speed",
            "Fall Speed",
            EffectParameterType.Speed,
            defaultValue: 4,
            minValue: 1,
            maxValue: 20
        ));

        _parameters.Add(new RangeParameter(
            "density",
            "Rain Density",
            EffectParameterType.Intensity,
            defaultValue: 100,
            minValue: 10,
            maxValue: 150
        ));

        _parameters.Add(new RangeParameter(
            "trailLength",
            "Trail Length",
            EffectParameterType.Intensity,
            defaultValue: 200,
            minValue: 100,
            maxValue: 400
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
        _noiseTime = 0;
        Array.Clear(_columnSpawnTimers);
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var backgroundColor = GetParameter<ColorParameter>("backgroundColor")?.ColorValue ?? Colors.Black;
        var colorParam = GetParameter<ColorParameter>("color");
        var speed = GetParameter<RangeParameter>("speed")?.NumericValue ?? 10;
        var density = GetParameter<RangeParameter>("density")?.NumericValue ?? 30;
        var trailLength = GetParameter<RangeParameter>("trailLength")?.NumericValue ?? 50;
        var colorVariation = GetParameter<RangeParameter>("colorVariation")?.NumericValue ?? 20;

        if (colorParam == null) return;

        var baseColor = colorParam.ColorValue;

        // Better scaling: use square root to make the adjustment more balanced
        var speedNormalized = speed / 20.0; // 0.05 to 1.0
        var speedFactor = Math.Sqrt(1.0 / speedNormalized); // Square root gives smoother scaling
        var baseDensity = (density / 100.0) * speedFactor * 0.15; // Convert to probability per frame

        _noiseTime += 0.05; // Advance noise time for moving pattern

        // Use noise-based spawning for more even distribution
        for (int col = 0; col < _keyboardService.MaxColumns; col++)
        {
            // Get noise value for this column (0-1 range after normalization)
            double noiseValue = (_noise.Noise(col * 0.3, _noiseTime) + 1.0) * 0.5;
            
            // Adjust spawn probability based on noise and density
            // Higher noise values = more likely to spawn
            double spawnThreshold = 1.0 - (baseDensity * noiseValue * 1.5);
            
            // Decrement timer for this column
            _columnSpawnTimers[col] -= 0.016; // Roughly 60 FPS
            
            // Check if we should spawn based on noise and timer
            if (_columnSpawnTimers[col] <= 0 && _random.NextDouble() > spawnThreshold)
            {
                _raindrops.Add(new Raindrop
                {
                    Column = col,
                    Row = 0,
                    Color = VaryColor(baseColor, colorVariation),
                    Speed = speed / 20.0
                });
                
                // Set cooldown timer - vary based on density (prevents too many drops per column)
                _columnSpawnTimers[col] = (1.0 / Math.Max(baseDensity * 2, 0.1)) * (0.5 + _random.NextDouble() * 0.5);
            }
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

        // Fill with background color
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                _colorBuffer[row, col] = new KeyColour(
                    backgroundColor.R,
                    backgroundColor.G,
                    backgroundColor.B
                );
            }
        }

        // Draw all trails with their specific colors on top of background
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
        var (h, s, v) = EffectUtilities.RgbToHsv(baseColor);

        // Vary the hue - at 100% variation, full 360 degree range for rainbow rain
        var variationAmount = variation / 100.0;
        var hueShift = (_random.NextDouble() - 0.5) * variationAmount * 720; // +/- 360 degrees at max variation
        h = (h + hueShift + 360) % 360; // Keep in 0-360 range

        // Convert back to RGB
        return EffectUtilities.HsvToRgb(h, s, v);
    }

    public override void Cleanup()
    {
        _raindrops.Clear();
        _trailData.Clear();
    }

    private class SimplexNoise
    {
        private static readonly int[][] Grad3 = 
        [
            [1,1,0], [-1,1,0], [1,-1,0], [-1,-1,0],
            [1,0,1], [-1,0,1], [1,0,-1], [-1,0,-1],
            [0,1,1], [0,-1,1], [0,1,-1], [0,-1,-1]
        ];

        private readonly byte[] _perm;
        private readonly byte[] _permMod12;

        public SimplexNoise(int seed = 0)
        {
            _perm = new byte[512];
            _permMod12 = new byte[512];
            
            var p = new byte[256];
            var random = new Random(seed);
            
            for (int i = 0; i < 256; i++)
                p[i] = (byte)i;

            for (int i = 255; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }

            for (int i = 0; i < 512; i++)
            {
                _perm[i] = p[i & 255];
                _permMod12[i] = (byte)(_perm[i] % 12);
            }
        }

        public double Noise(double xin, double yin)
        {
            const double F2 = 0.5 * (1.7320508075688772935 - 1.0);
            const double G2 = (3.0 - 1.7320508075688772935) / 6.0;

            double n0, n1, n2;

            double s = (xin + yin) * F2;
            int i = FastFloor(xin + s);
            int j = FastFloor(yin + s);
            double t = (i + j) * G2;
            double X0 = i - t;
            double Y0 = j - t;
            double x0 = xin - X0;
            double y0 = yin - Y0;

            int i1, j1;
            if (x0 > y0) { i1 = 1; j1 = 0; }
            else { i1 = 0; j1 = 1; }

            double x1 = x0 - i1 + G2;
            double y1 = y0 - j1 + G2;
            double x2 = x0 - 1.0 + 2.0 * G2;
            double y2 = y0 - 1.0 + 2.0 * G2;

            int ii = i & 255;
            int jj = j & 255;
            int gi0 = _permMod12[ii + _perm[jj]];
            int gi1 = _permMod12[ii + i1 + _perm[jj + j1]];
            int gi2 = _permMod12[ii + 1 + _perm[jj + 1]];

            double t0 = 0.5 - x0 * x0 - y0 * y0;
            if (t0 < 0) n0 = 0.0;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot(Grad3[gi0], x0, y0);
            }

            double t1 = 0.5 - x1 * x1 - y1 * y1;
            if (t1 < 0) n1 = 0.0;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Dot(Grad3[gi1], x1, y1);
            }

            double t2 = 0.5 - x2 * x2 - y2 * y2;
            if (t2 < 0) n2 = 0.0;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot(Grad3[gi2], x2, y2);
            }

            return 70.0 * (n0 + n1 + n2);
        }

        private static int FastFloor(double x)
        {
            int xi = (int)x;
            return x < xi ? xi - 1 : xi;
        }

        private static double Dot(int[] g, double x, double y)
        {
            return g[0] * x + g[1] * y;
        }
    }
}
