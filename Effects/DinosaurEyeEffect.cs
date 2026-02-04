using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;
using MediaColor = System.Windows.Media.Color;

namespace WootingRGB.Effects;

public class DinosaurEyeEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly Random _random = new();
    private readonly List<Raindrop> _raindrops = new();
    private double _nextRaindropTime = 0;
    private double _targetX = 0;
    private double _targetY = 0;
    private double _currentX = 0;
    private double _currentY = 0;

    // HID codes for directional keys
    private const short HID_W = 26;
    private const short HID_A = 4;
    private const short HID_S = 22;
    private const short HID_D = 7;
    private const short HID_ARROW_UP = 82;
    private const short HID_ARROW_LEFT = 80;
    private const short HID_ARROW_DOWN = 81;
    private const short HID_ARROW_RIGHT = 79;

    public override string Name => "Dinosaur Eye";
    public override string Description => "An eye-like radial gradient with a dark pupil center and random raindrop ripples";

    private class Raindrop
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public double Radius { get; set; }
        public double MaxRadius { get; set; }
        public double Speed { get; set; }
        public double Intensity { get; set; }
    }

    public DinosaurEyeEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "eyeColor",
            "Eye Color",
            MediaColor.FromRgb(0xBC, 0x87, 0x28) 
        ));

        _parameters.Add(new ColorParameter(
            "pupilColor",
            "Pupil Color",
            MediaColor.FromRgb(0x0D, 0x09, 0x00)
        ));

        _parameters.Add(new ColorParameter(
            "edgeColor",
            "Edge Color",
            MediaColor.FromRgb(0x08, 0x0B, 0x0D)
        ));

        _parameters.Add(new ColorParameter(
            "raindropColor",
            "Raindrop Color",
            MediaColor.FromRgb(0x6F, 0xC5, 0xFF)
        ));

        _parameters.Add(new RangeParameter(
            "pupilSize",
            "Pupil Size",
            EffectParameterType.Intensity,
            defaultValue: 53,
            minValue: 10,
            maxValue: 70
        ));

        _parameters.Add(new RangeParameter(
            "eyeRadius",
            "Eye Radius",
            EffectParameterType.Intensity,
            defaultValue: 27,
            minValue: 20,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "range",
            "Movement Range",
            EffectParameterType.Intensity,
            defaultValue: 7,
            minValue: 5,
            maxValue: 50
        ));

        _parameters.Add(new RangeParameter(
            "smoothing",
            "Movement Smoothing",
            EffectParameterType.Intensity,
            defaultValue: 85,
            minValue: 30,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "raindropFrequency",
            "Raindrop Frequency",
            EffectParameterType.Speed,
            defaultValue: 100,
            minValue: 5,
            maxValue: 150
        ));

        _parameters.Add(new RangeParameter(
            "raindropIntensity",
            "Raindrop Intensity",
            EffectParameterType.Intensity,
            defaultValue: 60,
            minValue: 10,
            maxValue: 100
        ));

        _parameters.Add(new BooleanParameter(
            "scaleToKeyboard",
            "Scale to Keyboard",
            defaultValue: true
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _raindrops.Clear();
        _nextRaindropTime = 0;
        _targetX = 0;
        _targetY = 0;
        _currentX = 0;
        _currentY = 0;
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var eyeColor = GetParameter<ColorParameter>("eyeColor")?.ColorValue ?? MediaColor.FromRgb(0xBC, 0x87, 0x28);
        var pupilColor = GetParameter<ColorParameter>("pupilColor")?.ColorValue ?? MediaColor.FromRgb(0x0D, 0x09, 0x00);
        var edgeColor = GetParameter<ColorParameter>("edgeColor")?.ColorValue ?? MediaColor.FromRgb(0x08, 0x0B, 0x0D);
        var raindropColor = GetParameter<ColorParameter>("raindropColor")?.ColorValue ?? MediaColor.FromRgb(0x6F, 0xC5, 0xFF);
        var pupilSize = GetParameter<RangeParameter>("pupilSize")?.NumericValue ?? 53;
        var eyeRadius = GetParameter<RangeParameter>("eyeRadius")?.NumericValue ?? 27;
        var range = GetParameter<RangeParameter>("range")?.NumericValue ?? 7;
        var smoothing = GetParameter<RangeParameter>("smoothing")?.NumericValue ?? 85;
        var raindropFrequency = GetParameter<RangeParameter>("raindropFrequency")?.NumericValue ?? 100;
        var raindropIntensity = GetParameter<RangeParameter>("raindropIntensity")?.NumericValue ?? 60;
        var scaleToKeyboard = GetParameter<BooleanParameter>("scaleToKeyboard")?.BooleanValue ?? true;

        // Calculate movement from WASD/Arrow keys
        double inputX = 0;
        double inputY = 0;

        foreach (var key in keyboardState.PressedKeys)
        {
            var pressure = Math.Clamp(key.Value, 0, 1);
            
            switch (key.Key)
            {
                case HID_W:
                case HID_ARROW_UP:
                    inputY -= pressure;
                    break;
                case HID_S:
                case HID_ARROW_DOWN:
                    inputY += pressure;
                    break;
                case HID_A:
                case HID_ARROW_LEFT:
                    inputX -= pressure;
                    break;
                case HID_D:
                case HID_ARROW_RIGHT:
                    inputX += pressure;
                    break;
            }
        }

        // Scale input by range
        double maxOffsetX, maxOffsetY;
        double rangeMultiplier = range / 50.0;
        
        if (scaleToKeyboard)
        {
            maxOffsetX = _keyboardService.MaxColumns * rangeMultiplier;
            maxOffsetY = _keyboardService.MaxRows * rangeMultiplier;
        }
        else
        {
            double maxOffset = Math.Max(_keyboardService.MaxRows, _keyboardService.MaxColumns) * rangeMultiplier;
            maxOffsetX = maxOffset;
            maxOffsetY = maxOffset;
        }
        
        _targetX = inputX * maxOffsetX;
        _targetY = inputY * maxOffsetY;

        // Smooth interpolation to target position
        double smoothingFactor = smoothing / 100.0;
        _currentX = EffectUtilities.Lerp(_targetX, _currentX, smoothingFactor);
        _currentY = EffectUtilities.Lerp(_targetY, _currentY, smoothingFactor);

        // Calculate eye center with offset
        double keyboardCenterRow = _keyboardService.MaxRows / 2.0;
        double keyboardCenterCol = _keyboardService.MaxColumns / 2.0;
        double eyeCenterRow = keyboardCenterRow + _currentY;
        double eyeCenterCol = keyboardCenterCol + _currentX;

        double pupilOffsetX = _currentX * 0.6;
        double pupilOffsetY = _currentY * 0.6;
        double pupilCenterRow = eyeCenterRow + pupilOffsetY;
        double pupilCenterCol = eyeCenterCol + pupilOffsetX;

        double eyeRadiusValue = Math.Max(_keyboardService.MaxRows, _keyboardService.MaxColumns) * (eyeRadius / 100.0);
        double pupilRadiusValue = eyeRadiusValue * (pupilSize / 100.0);

        // Spawn random raindrops
        _nextRaindropTime -= 0.016;
        if (_nextRaindropTime <= 0)
        {
            var randomRow = _random.Next(_keyboardService.MaxRows);
            var randomCol = _random.Next(_keyboardService.MaxColumns);

            _raindrops.Add(new Raindrop
            {
                Row = randomRow,
                Col = randomCol,
                Radius = 0,
                MaxRadius = 2.5 + _random.NextDouble() * 2.5,
                Speed = 0.15,
                Intensity = (raindropIntensity / 100.0) * 0.6
            });

            var frequencyFactor = 1.0 / Math.Max(raindropFrequency / 100.0, 0.1);
            _nextRaindropTime = frequencyFactor * (0.5 + _random.NextDouble() * 0.5);
        }

        // Update raindrops
        var dropsToRemove = new List<Raindrop>();
        foreach (var drop in _raindrops)
        {
            drop.Radius += drop.Speed;
            
            // Fade based on progress
            double progress = drop.Radius / drop.MaxRadius;
            drop.Intensity = (raindropIntensity / 100.0) * 0.6 * (1.0 - progress);

            if (drop.Radius >= drop.MaxRadius)
            {
                dropsToRemove.Add(drop);
            }
        }

        foreach (var drop in dropsToRemove)
        {
            _raindrops.Remove(drop);
        }

        // Render the effect
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                double distanceFromEyeCenter = EffectUtilities.Distance(col, row, eyeCenterCol, eyeCenterRow);
                double distanceFromPupilCenter = EffectUtilities.Distance(col, row, pupilCenterCol, pupilCenterRow);

                MediaColor baseColor;

                if (distanceFromPupilCenter <= pupilRadiusValue)
                {
                    baseColor = pupilColor;
                }
                else if (distanceFromEyeCenter <= eyeRadiusValue)
                {
                    double normalizedDistance = (distanceFromEyeCenter - pupilRadiusValue) / (eyeRadiusValue - pupilRadiusValue);
                    double intensity = 1.0 - Math.Pow(normalizedDistance, 3.5);
                    baseColor = EffectUtilities.LerpColor(edgeColor, eyeColor, intensity);
                }
                else
                {
                    baseColor = edgeColor;
                }

                // Calculate raindrop influence
                double raindropEffect = 0;
                foreach (var drop in _raindrops)
                {
                    var dropDistance = Math.Sqrt(
                        Math.Pow(row - drop.Row, 2) + 
                        Math.Pow(col - drop.Col, 2)
                    );

                    var ringThickness = 1.2;
                    var distanceFromRing = Math.Abs(dropDistance - drop.Radius);

                    if (distanceFromRing < ringThickness)
                    {
                        var ringIntensity = (1.0 - (distanceFromRing / ringThickness)) * drop.Intensity;
                        raindropEffect = Math.Max(raindropEffect, ringIntensity);
                    }
                }

                // Blend raindrop with base color
                MediaColor finalColor;
                if (raindropEffect > 0)
                {
                    finalColor = MediaColor.FromRgb(
                        (byte)Math.Clamp(baseColor.R * (1 - raindropEffect) + raindropColor.R * raindropEffect, 0, 255),
                        (byte)Math.Clamp(baseColor.G * (1 - raindropEffect) + raindropColor.G * raindropEffect, 0, 255),
                        (byte)Math.Clamp(baseColor.B * (1 - raindropEffect) + raindropColor.B * raindropEffect, 0, 255)
                    );
                }
                else
                {
                    finalColor = baseColor;
                }

                _colorBuffer[row, col] = new KeyColour(finalColor.R, finalColor.G, finalColor.B);
            }
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    public override void Cleanup()
    {
        _raindrops.Clear();
        _nextRaindropTime = 0;
        _targetX = 0;
        _targetY = 0;
        _currentX = 0;
        _currentY = 0;
    }
}
