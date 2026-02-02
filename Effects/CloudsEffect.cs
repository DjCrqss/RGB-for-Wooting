using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;
using MediaColor = System.Windows.Media.Color;

namespace WootingRGB.Effects;

public class CloudsEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly SimplexNoise _simplexNoise;
    private double _time = 0;

    public override string Name => "Clouds";
    public override string Description => "Drifting clouds over a gradient sky background";

    public CloudsEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
        _simplexNoise = new SimplexNoise();
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "skyColor1",
            "Sky Color (Top)",
            MediaColor.FromRgb(0x87, 0xBF, 0xFF) // #87BFFF - Light blue
        ));

        _parameters.Add(new ColorParameter(
            "skyColor2",
            "Sky Color (Bottom)",
            MediaColor.FromRgb(0x11, 0x1F, 0x66) // #111F66 - Dark blue
        ));

        _parameters.Add(new RangeParameter(
            "speed",
            "Cloud Speed",
            EffectParameterType.Speed,
            defaultValue: 11,
            minValue: 1,
            maxValue: 25
        ));

        _parameters.Add(new RangeParameter(
            "density",
            "Cloud Density",
            EffectParameterType.Size,
            defaultValue: 100,
            minValue: 75,
            maxValue: 125
        ));

        _parameters.Add(new RangeParameter(
            "threshold",
            "Cloud Threshold",
            EffectParameterType.Intensity,
            defaultValue: 50,
            minValue: 1,
            maxValue: 99
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _time = 0;
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var skyColor1 = GetParameter<ColorParameter>("skyColor1")?.ColorValue ?? MediaColor.FromRgb(0, 123, 255);
        var skyColor2 = GetParameter<ColorParameter>("skyColor2")?.ColorValue ?? MediaColor.FromRgb(135, 206, 250);
        var speed = GetParameter<RangeParameter>("speed")?.NumericValue ?? 20;
        var density = GetParameter<RangeParameter>("density")?.NumericValue ?? 50;
        var threshold = GetParameter<RangeParameter>("threshold")?.NumericValue ?? 50;

        _time += speed * 0.001;

        var scaleValue = (100 - density + 20) / 100.0; // Inverse density for noise scale
        var thresholdValue = threshold / 100.0; // Convert to 0-1 range

        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                // Calculate vertical gradient for sky (top to bottom)
                double verticalPosition = (double)row / (_keyboardService.MaxRows - 1);
                var skyColor = EffectUtilities.LerpColor(skyColor1, skyColor2, verticalPosition);

                // Generate Simplex noise across whole keyboard
                // Stretch X axis by 0.5 to make clouds wider horizontally
                var noiseX = (col * scaleValue + _time) * 0.5;
                var noiseY = row * scaleValue;
                
                var noiseValue = _simplexNoise.Noise(noiseX, noiseY);
                noiseValue = (noiseValue + 1) * 0.5; // Normalize to 0-1

                // Only show clouds where noise value is above threshold
                if (noiseValue > thresholdValue)
                {
                    // Map threshold-1.0 range to 0-1 for cloud opacity
                    var cloudOpacity = (noiseValue - thresholdValue) / (1.0 - thresholdValue);
                    
                    // Apply inverse power curve to make most clouds solid white with soft edges
                    // This makes the curve start high (white) and taper off slowly
                    cloudOpacity = 1.0 - Math.Pow(1.0 - cloudOpacity, 3.0);
                    
                    // Interpolate between sky color and white based on cloud opacity
                    var cloudColor = MediaColor.FromRgb(255, 255, 255);
                    var finalColor = EffectUtilities.LerpColor(skyColor, cloudColor, cloudOpacity);

                    _colorBuffer[row, col] = new KeyColour(
                        finalColor.R,
                        finalColor.G,
                        finalColor.B
                    );
                }
                else
                {
                    // Pure gradient background where no clouds
                    _colorBuffer[row, col] = new KeyColour(
                        skyColor.R,
                        skyColor.G,
                        skyColor.B
                    );
                }
            }
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    public override void Cleanup()
    {
        // Nothing to clean up
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
