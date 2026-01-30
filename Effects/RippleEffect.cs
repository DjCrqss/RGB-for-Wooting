using System.Windows.Media;
using System.Collections.Concurrent;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;
using MediaColor = System.Windows.Media.Color;
using System.Diagnostics;

namespace WootingRGB.Effects;

public class RippleEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private readonly ConcurrentDictionary<short, double> _deepestPressure = new();
    private readonly ConcurrentDictionary<short, double> _lastPressure = new();
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
            MediaColor.FromRgb(0x6F, 0xC5, 0xFF) // #6FC5FF
        ));

        _parameters.Add(new ColorParameter(
            "backgroundColor",
            "Background Color",
            MediaColor.FromRgb(0x14, 0x26, 0x57) // #142657
        ));

        _parameters.Add(new RangeParameter(
            "spreadSpeed",
            "Spread Speed",
            EffectParameterType.Speed,
            defaultValue: 13,
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
            "velocityInfluence",
            "Velocity Influence",
            EffectParameterType.Intensity,
            defaultValue: 75,
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
        _lastPressure.Clear();
        lock (_ripplesLock)
        {
            _activeRipples.Clear();
        }
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var rippleColor = GetParameter<ColorParameter>("rippleColor")?.ColorValue ?? MediaColor.FromRgb(0, 255, 255);
        var backgroundColor = GetParameter<ColorParameter>("backgroundColor")?.ColorValue ?? MediaColor.FromRgb(0, 0, 0);
        var spreadSpeed = GetParameter<RangeParameter>("spreadSpeed")?.NumericValue ?? 13;
        var fadeSpeed = GetParameter<RangeParameter>("fadeSpeed")?.NumericValue ?? 45;
        var velocityInfluence = GetParameter<RangeParameter>("velocityInfluence")?.NumericValue ?? 50;
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
                // Get last pressure for this key
                var lastPressure = _lastPressure.GetValueOrDefault(keyCode, 0);
                
                // Check if this is a new press or if pressure changed
                if (_deepestPressure.TryGetValue(keyCode, out var deepest))
                {
                    // If pressure increased from last frame, update deepest and reset
                    if (pressure > lastPressure)
                    {
                        _deepestPressure[keyCode] = pressure;
                    }
                    // If pressure decreased significantly from deepest, trigger ripple
                    else if (pressure < deepest - 0.15)
                    {
                        // Get key position
                        if (WootingAnalogLedMapping.HidToPosition.TryGetValue(keyCode, out var position))
                        {
                            var (row, col) = position;
                            
                            if (row >= 0 && row < _keyboardService.MaxRows && 
                                col >= 0 && col < _keyboardService.MaxColumns)
                            {
                                // Calculate release velocity based on pressure drop
                                var pressureDrop = deepest - pressure;
                                var velocityMultiplier = 1 + (pressureDrop * (velocityInfluence / 50.0) * 4.0);
                                velocityMultiplier = Math.Clamp(velocityMultiplier, 0.5, 6.0);

                                // Create ripple
                                var ripple = new Ripple
                                {
                                    Row = row,
                                    Col = col,
                                    Radius = 0,
                                    MaxRadius = 5 + (deepest * 15),
                                    Speed = (spreadSpeed / 100.0) * 0.5 * velocityMultiplier,
                                    Intensity = deepest,
                                    Color = useRandomColors ? GetRandomColor() : rippleColor
                                };

                                lock (_ripplesLock)
                                {
                                    _activeRipples.Add(ripple);
                                }
                                
                                // Reset deepest for this key to current pressure
                                _deepestPressure[keyCode] = 0;
                            }
                        }
                    }
                }
                else
                {
                    // New key press - start tracking
                    _deepestPressure[keyCode] = pressure;
                }
                
                // Update last pressure
                _lastPressure[keyCode] = pressure;
            }
        }

        // Clean up keys that are no longer pressed
        var currentKeys = new HashSet<short>(keyboardState.PressedKeys.Keys);
        var keysToRemove = _deepestPressure.Keys.Where(k => !currentKeys.Contains(k)).ToList();
        
        foreach (var keyCode in keysToRemove)
        {
            // Key was released - always trigger a ripple if we were tracking it
            if (_deepestPressure.TryGetValue(keyCode, out var deepest))
            {
                if (WootingAnalogLedMapping.HidToPosition.TryGetValue(keyCode, out var position))
                {
                    var (row, col) = position;
                    
                    if (row >= 0 && row < _keyboardService.MaxRows && 
                        col >= 0 && col < _keyboardService.MaxColumns)
                    {
                        // Use deepest as the pressure drop (fast release from deepest to 0)
                        var pressureDrop = deepest;
                        var velocityMultiplier = 1.0 + (pressureDrop * (velocityInfluence / 50.0) * 4.0);
                        velocityMultiplier = Math.Clamp(velocityMultiplier, 0.5, 3.0);

                        var ripple = new Ripple
                        {
                            Row = row,
                            Col = col,
                            Radius = 0,
                            MaxRadius = 5 + (deepest * 15),
                            Speed = (spreadSpeed / 100.0) * 0.5 * velocityMultiplier,
                            Intensity = deepest,
                            Color = useRandomColors ? GetRandomColor() : rippleColor
                        };

                        lock (_ripplesLock)
                        {
                            _activeRipples.Add(ripple);
                        }
                    }
                }
            }
            
            _deepestPressure.TryRemove(keyCode, out _);
            _lastPressure.TryRemove(keyCode, out _);
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
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                double rippleIntensity = 0;
                MediaColor activeRippleColor = backgroundColor;

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
                            
                            if (ringIntensity > rippleIntensity)
                            {
                                rippleIntensity = ringIntensity;
                                activeRippleColor = ripple.Color;
                            }
                        }
                    }
                }

                // Blend ripple with background based on ripple intensity
                MediaColor finalColor;
                if (rippleIntensity > 0)
                {
                    // Interpolate between background and ripple color
                    finalColor = MediaColor.FromRgb(
                        (byte)(backgroundColor.R * (1 - rippleIntensity) + activeRippleColor.R * rippleIntensity),
                        (byte)(backgroundColor.G * (1 - rippleIntensity) + activeRippleColor.G * rippleIntensity),
                        (byte)(backgroundColor.B * (1 - rippleIntensity) + activeRippleColor.B * rippleIntensity)
                    );
                }
                else
                {
                    finalColor = backgroundColor;
                }

                _colorBuffer[row, col] = new KeyColour(finalColor.R, finalColor.G, finalColor.B);
            }
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    private MediaColor GetRandomColor()
    {
        var random = new Random();
        var hue = random.NextDouble() * 360;
        return EffectUtilities.HsvToRgb(hue, 1.0, 1.0);
    }

    public override void Cleanup()
    {
        _deepestPressure.Clear();
        _lastPressure.Clear();
        lock (_ripplesLock)
        {
            _activeRipples.Clear();
        }
    }
}
