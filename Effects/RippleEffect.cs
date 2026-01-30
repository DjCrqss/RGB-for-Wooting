using System.Windows.Media;
using System.Collections.Concurrent;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;
using MediaColor = System.Windows.Media.Color;

namespace WootingRGB.Effects;

public class RippleEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly ConcurrentDictionary<short, double> _deepestPressure = new();
    private readonly List<Ripple> _activeRipples = new();
    private readonly object _ripplesLock = new();

    public override string Name => "Ripple";
    public override string Description => "Ripples emanate from keys on release based on pressure";

    private class Ripple
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public double Radius { get; set; }
        public double MaxRadius { get; set; }
        public double Speed { get; set; }
        public double Intensity { get; set; }
        public MediaColor Color { get; set; }
    }

    public RippleEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "rippleColor",
            "Ripple Color",
            MediaColor.FromRgb(0x00, 0xFF, 0xFF) // Cyan
        ));

        _parameters.Add(new RangeParameter(
            "spreadSpeed",
            "Spread Speed",
            EffectParameterType.Speed,
            defaultValue: 20,
            minValue: 1,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "fadeSpeed",
            "Fade Speed",
            EffectParameterType.Speed,
            defaultValue: 45,
            minValue: 1,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "baseBrightness",
            "Base Brightness",
            EffectParameterType.Intensity,
            defaultValue: 12,
            minValue: 0,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "randomColors",
            "Random Colors",
            EffectParameterType.Intensity,
            defaultValue: 0,
            minValue: 0,
            maxValue: 1
        ));

        _parameters.Add(new RangeParameter(
            "inverted",
            "Inverted",
            EffectParameterType.Intensity,
            defaultValue: 0,
            minValue: 0,
            maxValue: 1
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _deepestPressure.Clear();
        lock (_ripplesLock)
        {
            _activeRipples.Clear();
        }
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var rippleColor = GetParameter<ColorParameter>("rippleColor")?.ColorValue ?? MediaColor.FromRgb(0, 255, 255);
        var spreadSpeed = GetParameter<RangeParameter>("spreadSpeed")?.NumericValue ?? 20;
        var fadeSpeed = GetParameter<RangeParameter>("fadeSpeed")?.NumericValue ?? 45;
        var baseBrightness = GetParameter<RangeParameter>("baseBrightness")?.NumericValue ?? 12;
        var randomColors = GetParameter<RangeParameter>("randomColors")?.NumericValue ?? 0;
        var inverted = GetParameter<RangeParameter>("inverted")?.NumericValue ?? 0;

        bool useRandomColors = randomColors > 0.5;
        bool isInverted = inverted > 0.5;

        // Track currently pressed keys and detect releases
        foreach (var pressedKey in keyboardState.PressedKeys)
        {
            var keyCode = pressedKey.Key;
            var pressure = Math.Clamp(pressedKey.Value, 0, 1);
            
            // Only track keys with significant pressure
            if (pressure > 0.01)
            {
                // Check if this is a new press or if pressure decreased (release starting)
                if (_deepestPressure.TryGetValue(keyCode, out var deepest))
                {
                    // If pressure decreased from deepest, trigger ripple
                    if (pressure < deepest - 0.05) // Threshold to avoid noise
                    {
                        // Get key position
                        if (WootingAnalogLedMapping.HidToPosition.TryGetValue(keyCode, out var position))
                        {
                            var (row, col) = position;
                            
                            if (row >= 0 && row < _keyboardService.MaxRows && 
                                col >= 0 && col < _keyboardService.MaxColumns)
                            {
                                // Create ripple based on deepest pressure
                                var ripple = new Ripple
                                {
                                    Row = row,
                                    Col = col,
                                    Radius = 0,
                                    MaxRadius = 5 + (deepest * 15), // 5-20 based on deepest pressure
                                    Speed = (spreadSpeed / 100.0) * 0.5 * (0.5 + deepest * 0.5), // Faster with more pressure
                                    Intensity = deepest, // Use deepest pressure for brightness
                                    Color = useRandomColors ? GetRandomColor() : rippleColor
                                };

                                lock (_ripplesLock)
                                {
                                    _activeRipples.Add(ripple);
                                }
                            }
                        }
                        
                        // Reset tracking for this key
                        _deepestPressure.TryRemove(keyCode, out _);
                    }
                    else
                    {
                        // Update deepest pressure if current is higher
                        if (pressure > deepest)
                        {
                            _deepestPressure[keyCode] = pressure;
                        }
                    }
                }
                else
                {
                    // New key press - start tracking
                    _deepestPressure[keyCode] = pressure;
                }
            }
        }

        // Clean up keys that are no longer pressed at all
        var currentKeys = new HashSet<short>(keyboardState.PressedKeys.Keys);
        var keysToRemove = _deepestPressure.Keys.Where(k => !currentKeys.Contains(k)).ToList();
        
        foreach (var keyCode in keysToRemove)
        {
            var deepest = _deepestPressure[keyCode];
            
            // Create final ripple on complete release
            if (WootingAnalogLedMapping.HidToPosition.TryGetValue(keyCode, out var position))
            {
                var (row, col) = position;
                
                if (row >= 0 && row < _keyboardService.MaxRows && 
                    col >= 0 && col < _keyboardService.MaxColumns)
                {
                    var ripple = new Ripple
                    {
                        Row = row,
                        Col = col,
                        Radius = 0,
                        MaxRadius = 5 + (deepest * 15),
                        Speed = (spreadSpeed / 100.0) * 0.5 * (0.5 + deepest * 0.5),
                        Intensity = deepest,
                        Color = useRandomColors ? GetRandomColor() : rippleColor
                    };

                    lock (_ripplesLock)
                    {
                        _activeRipples.Add(ripple);
                    }
                }
            }
            
            _deepestPressure.TryRemove(keyCode, out _);
        }

        // Update ripples
        var ripplesToRemove = new List<Ripple>();
        lock (_ripplesLock)
        {
            foreach (var ripple in _activeRipples)
            {
                ripple.Radius += ripple.Speed;
                ripple.Intensity -= (fadeSpeed / 100.0) * 0.02;

                if (ripple.Intensity <= 0 || ripple.Radius > ripple.MaxRadius)
                {
                    ripplesToRemove.Add(ripple);
                }
            }

            foreach (var ripple in ripplesToRemove)
            {
                _activeRipples.Remove(ripple);
            }
        }

        // Render
        var baseIntensity = baseBrightness / 100.0;
        
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                double totalIntensity = baseIntensity;
                MediaColor finalColor = MediaColor.FromRgb(0, 0, 0);

                lock (_ripplesLock)
                {
                    foreach (var ripple in _activeRipples)
                    {
                        var distance = Math.Sqrt(
                            Math.Pow(row - ripple.Row, 2) + 
                            Math.Pow(col - ripple.Col, 2)
                        );

                        // Ripple is a ring, not a filled circle
                        var ringThickness = 1.5;
                        var distanceFromRing = Math.Abs(distance - ripple.Radius);

                        if (distanceFromRing < ringThickness)
                        {
                            // Calculate intensity based on distance from ring center
                            var ringIntensity = (1.0 - (distanceFromRing / ringThickness)) * ripple.Intensity;
                            
                            if (ringIntensity > totalIntensity)
                            {
                                totalIntensity = ringIntensity;
                                finalColor = ripple.Color;
                            }
                        }
                    }
                }

                // Apply inverted mode if enabled
                if (isInverted && totalIntensity > baseIntensity)
                {
                    totalIntensity = baseIntensity + (1.0 - totalIntensity);
                }

                totalIntensity = Math.Clamp(totalIntensity, 0, 1);

                _colorBuffer[row, col] = new KeyColour(
                    (byte)(finalColor.R * totalIntensity),
                    (byte)(finalColor.G * totalIntensity),
                    (byte)(finalColor.B * totalIntensity)
                );
            }
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    private MediaColor GetRandomColor()
    {
        var random = new Random();
        var hue = random.NextDouble() * 360;
        return HsvToRgb(hue, 1.0, 1.0);
    }

    private MediaColor HsvToRgb(double h, double s, double v)
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

        return MediaColor.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255)
        );
    }

    public override void Cleanup()
    {
        _deepestPressure.Clear();
        lock (_ripplesLock)
        {
            _activeRipples.Clear();
        }
    }
}
