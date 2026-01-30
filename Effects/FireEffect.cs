using System.Diagnostics;
using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;

namespace WootingRGB.Effects;

public class FireEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly Random _random = new();
    private double[,]? _heatMap;
    private double[]? _noiseOffsets;
    private double _time = 0;

    public override string Name => "Fire";
    public override string Description => "Flickering fire effect";

    public FireEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "coldColor",
            "Cold Color",
            Colors.Red
        ));

        _parameters.Add(new ColorParameter(
            "hotColor",
            "Hot Color",
            Colors.Yellow
        ));

        _parameters.Add(new RangeParameter(
            "height",
            "Flame Height",
            EffectParameterType.Size,
            defaultValue: 25.0,
            minValue: 10.0,
            maxValue: 50.0
        ));

        _parameters.Add(new RangeParameter(
            "speed",
            "Animation Speed",
            EffectParameterType.Speed,
            defaultValue: 10,
            minValue: 1,
            maxValue: 20
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _heatMap = new double[_keyboardService!.MaxRows, _keyboardService.MaxColumns];
        _noiseOffsets = new double[_keyboardService.MaxColumns];
        
        // Initialize random noise offsets for each column
        for (int col = 0; col < _keyboardService.MaxColumns; col++)
        {
            _noiseOffsets[col] = _random.NextDouble() * 100;
        }
        
        _time = 0;
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null || _heatMap == null || _noiseOffsets == null) return;

        var coldColor = GetParameter<ColorParameter>("coldColor")?.ColorValue ?? Colors.Red;
        var hotColor = GetParameter<ColorParameter>("hotColor")?.ColorValue ?? Colors.Yellow;
        var height = GetParameter<RangeParameter>("height")?.NumericValue ?? 25.0;
        var speed = GetParameter<RangeParameter>("speed")?.NumericValue ?? 10;

        _time += speed * 0.01;

        for (int col = 0; col < _keyboardService!.MaxColumns; col++)
        {
            // Create smooth noise using sine waves
            var noiseValue = Math.Sin(_time + _noiseOffsets[col]) * 0.5 + 0.5; // 0 to 1
            var noiseValue2 = Math.Sin(_time * 1.7 + _noiseOffsets[col] * 0.7) * 0.5 + 0.5;
            var combinedNoise = (noiseValue * 0.6 + noiseValue2 * 0.4);
            
            // Set base heat (0.5 to 1.0)
            _heatMap[_keyboardService.MaxRows - 1, col] = 0.5 + combinedNoise * 0.5;
        }


        // Propagate heat upward with cooling
        for (int row = _keyboardService.MaxRows - 2; row >= 0; row--)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                var heatBelow = _heatMap[row + 1, col];
                
                // propagate if heat below is above threshold
                if (heatBelow > 0.25)
                {
                    _heatMap[row, col] = (heatBelow - ((50 - height) * 0.01));
                }
                else
                {
                    // cool in place
                    _heatMap[row, col] = _heatMap[row, col] * 0.85;
                }
                
                _heatMap[row, col] = Math.Clamp(_heatMap[row, col], 0, 1);
            }
        }

        // Apply gradient with smooth fade to black
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                var heat = _heatMap[row, col];
                
                Color color;
                if (heat < 0.25)
                {
                    var fadeProgress = heat / 0.25; // 0 to 1
                    color = EffectUtilities.LerpColor(Colors.Black, coldColor, fadeProgress);
                }
                else
                {
                    var flameProgress = (heat - 0.25) / 0.75; // 0 to 1
                    color = EffectUtilities.LerpColor(coldColor, hotColor, flameProgress);
                }

                _colorBuffer[row, col] = new KeyColour(color.R, color.G, color.B);
            }
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    public override void Cleanup()
    {
        _heatMap = null;
        _noiseOffsets = null;
    }
}
