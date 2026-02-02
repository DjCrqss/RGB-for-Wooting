using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;
using MediaColor = System.Windows.Media.Color;

namespace WootingRGB.Effects;

public class VoronoiEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly VoronoiNoise _voronoiNoise;
    private double _time = 0;

    public override string Name => "Voronoi";
    public override string Description => "Cellular pattern with animated Voronoi noise";

    public VoronoiEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
        _voronoiNoise = new VoronoiNoise();
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "color1",
            "Cell Color",
            MediaColor.FromRgb(0x00, 0x00, 0x00) // #000000 - Black
        ));

        _parameters.Add(new ColorParameter(
            "color2",
            "Border Color",
            MediaColor.FromRgb(0x00, 0xFF, 0xFF) // #00FFFF - Cyan
        ));

        _parameters.Add(new RangeParameter(
            "speed",
            "Animation Speed",
            EffectParameterType.Speed,
            defaultValue: 3,
            minValue: 1,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "cellSize",
            "Cell Size",
            EffectParameterType.Size,
            defaultValue: 59,
            minValue: 10,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "borderWidth",
            "Border Width",
            EffectParameterType.Intensity,
            defaultValue: 50,
            minValue: 1,
            maxValue: 50
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

        var color1 = GetParameter<ColorParameter>("color1")?.ColorValue ?? MediaColor.FromRgb(255, 0, 255);
        var color2 = GetParameter<ColorParameter>("color2")?.ColorValue ?? MediaColor.FromRgb(0, 255, 255);
        var speed = GetParameter<RangeParameter>("speed")?.NumericValue ?? 20;
        var cellSize = GetParameter<RangeParameter>("cellSize")?.NumericValue ?? 50;
        var borderWidth = GetParameter<RangeParameter>("borderWidth")?.NumericValue ?? 15;

        _time += speed * 0.001;

        var scaleValue = (100 - cellSize + 20) / 100.0;
        var borderThreshold = borderWidth / 100.0;

        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                var x = col * scaleValue + _time;
                var y = row * scaleValue;

                // Get Voronoi noise value (distance to nearest cell center)
                var (cellValue, edgeDistance) = _voronoiNoise.Noise(x, y);

                // Create color based on edge distance
                // edgeDistance is 0 at borders, 1 at cell centers
                MediaColor finalColor;
                if (edgeDistance < borderThreshold)
                {
                    // We're near a border
                    var borderBlend = edgeDistance / borderThreshold;
                    finalColor = EffectUtilities.LerpColor(color2, color1, borderBlend);
                }
                else
                {
                    // We're in the cell center
                    finalColor = color1;
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
        // Nothing to clean up
    }

    private class VoronoiNoise
    {
        private readonly Random _random;
        private const int GridSize = 256;
        private readonly (double x, double y)[,] _points;

        public VoronoiNoise(int seed = 42)
        {
            _random = new Random(seed);
            _points = new (double, double)[GridSize, GridSize];

            // Generate random points in each grid cell
            for (int i = 0; i < GridSize; i++)
            {
                for (int j = 0; j < GridSize; j++)
                {
                    _points[i, j] = (
                        _random.NextDouble(),
                        _random.NextDouble()
                    );
                }
            }
        }

        public (double cellValue, double edgeDistance) Noise(double x, double y)
        {
            // Get the grid cell coordinates
            int cellX = FastFloor(x);
            int cellY = FastFloor(y);

            // Position within the cell (0-1)
            double localX = x - cellX;
            double localY = y - cellY;

            // Find the two closest points
            double minDist1 = double.MaxValue;
            double minDist2 = double.MaxValue;

            // Check the current cell and surrounding 8 cells
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int neighborX = (cellX + dx) & (GridSize - 1);
                    int neighborY = (cellY + dy) & (GridSize - 1);

                    var point = _points[neighborX, neighborY];
                    
                    // Calculate distance to this point
                    double pointX = dx + point.x;
                    double pointY = dy + point.y;
                    double dist = Distance(localX, localY, pointX, pointY);

                    // Keep track of the two closest distances
                    if (dist < minDist1)
                    {
                        minDist2 = minDist1;
                        minDist1 = dist;
                    }
                    else if (dist < minDist2)
                    {
                        minDist2 = dist;
                    }
                }
            }

            // Cell value is based on closest point distance
            double cellValue = minDist1;

            // Edge distance is the difference between the two closest points
            // Normalized to approximately 0-1 range
            double edgeDistance = (minDist2 - minDist1) * 2.0;
            edgeDistance = Math.Clamp(edgeDistance, 0.0, 1.0);

            return (cellValue, edgeDistance);
        }

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static int FastFloor(double x)
        {
            int xi = (int)x;
            return x < xi ? xi - 1 : xi;
        }
    }
}
