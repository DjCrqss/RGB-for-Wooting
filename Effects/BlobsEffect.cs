using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using WootingRGB.lib;
using MediaColor = System.Windows.Media.Color;

namespace WootingRGB.Effects;

public class BlobsEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    private List<Blob> _blobs = new();
    private Random _random = new();

    public override string Name => "Blobs";
    public override string Description => "Floating blobs that react to key presses";

    public BlobsEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new RangeParameter(
            "blobCount",
            "Blob Count",
            EffectParameterType.Intensity,
            defaultValue: 8,
            minValue: 1,
            maxValue: 20
        ));

        _parameters.Add(new ColorParameter(
            "smallColor",
            "Small Blob Color",
            MediaColor.FromRgb(0xFF, 0x00, 0xFF) // Magenta
        ));

        _parameters.Add(new ColorParameter(
            "largeColor",
            "Large Blob Color",
            MediaColor.FromRgb(0x00, 0xFF, 0xFF) // Cyan
        ));

        _parameters.Add(new RangeParameter(
            "speed",
            "Float Speed",
            EffectParameterType.Speed,
            defaultValue: 30,
            minValue: 1,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "minSize",
            "Minimum Size",
            EffectParameterType.Size,
            defaultValue: 10,
            minValue: 10,
            maxValue: 50
        ));

        _parameters.Add(new RangeParameter(
            "maxSize",
            "Maximum Size",
            EffectParameterType.Size,
            defaultValue: 40,
            minValue: 30,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "blur",
            "Blur Factor",
            EffectParameterType.Intensity,
            defaultValue: 70,
            minValue: 10,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "attraction",
            "Key Attraction",
            EffectParameterType.Intensity,
            defaultValue: 100,
            minValue: 0,
            maxValue: 100
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _blobs.Clear();
        InitializeBlobs();
    }

    private void InitializeBlobs()
    {
        var blobCount = (int)(GetParameter<RangeParameter>("blobCount")?.NumericValue ?? 8);
        var minSize = GetParameter<RangeParameter>("minSize")?.NumericValue ?? 20;
        var maxSize = GetParameter<RangeParameter>("maxSize")?.NumericValue ?? 60;

        for (int i = 0; i < blobCount; i++)
        {
            _blobs.Add(new Blob
            {
                X = _random.NextDouble() * _keyboardService.MaxColumns,
                Y = _random.NextDouble() * _keyboardService.MaxRows,
                VelocityX = (_random.NextDouble() - 0.5) * 0.1,
                VelocityY = (_random.NextDouble() - 0.5) * 0.1,
                Radius = minSize + _random.NextDouble() * (maxSize - minSize)
            });
        }
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var blobCount = (int)(GetParameter<RangeParameter>("blobCount")?.NumericValue ?? 8);
        var smallColor = GetParameter<ColorParameter>("smallColor")?.ColorValue ?? MediaColor.FromRgb(255, 0, 255);
        var largeColor = GetParameter<ColorParameter>("largeColor")?.ColorValue ?? MediaColor.FromRgb(0, 255, 255);
        var speed = (GetParameter<RangeParameter>("speed")?.NumericValue ?? 30) / 100.0;
        var minSize = GetParameter<RangeParameter>("minSize")?.NumericValue ?? 20;
        var maxSize = GetParameter<RangeParameter>("maxSize")?.NumericValue ?? 60;
        var blurFactor = (GetParameter<RangeParameter>("blur")?.NumericValue ?? 70) / 100.0;
        var attraction = (GetParameter<RangeParameter>("attraction")?.NumericValue ?? 50) / 100.0;

        // Adjust blob count if changed
        while (_blobs.Count < blobCount)
        {
            _blobs.Add(new Blob
            {
                X = _random.NextDouble() * _keyboardService.MaxColumns,
                Y = _random.NextDouble() * _keyboardService.MaxRows,
                VelocityX = (_random.NextDouble() - 0.5) * 0.1,
                VelocityY = (_random.NextDouble() - 0.5) * 0.1,
                Radius = minSize + _random.NextDouble() * (maxSize - minSize)
            });
        }
        while (_blobs.Count > blobCount)
        {
            _blobs.RemoveAt(_blobs.Count - 1);
        }

        // Update blob sizes if parameters changed
        for (int i = 0; i < _blobs.Count; i++)
        {
            var blob = _blobs[i];
            if (blob.Radius < minSize || blob.Radius > maxSize)
            {
                // Re-randomize size within new range
                var normalizedSize = (blob.Radius - minSize) / Math.Max(1, maxSize - minSize);
                blob.Radius = minSize + normalizedSize * (maxSize - minSize);
                blob.Radius = Math.Clamp(blob.Radius, minSize, maxSize);
            }
        }

        // Get pressed key positions
        var pressedPositions = new List<(int row, int col, float pressure)>();
        foreach (var pressedKey in keyboardState.PressedKeys)
        {
            if (WootingAnalogLedMapping.HidToPosition.TryGetValue(pressedKey.Key, out var position))
            {
                pressedPositions.Add((position.row, position.col, pressedKey.Value));
            }
        }

        // Update blob actual positions (natural floating)
        foreach (var blob in _blobs)
        {
            // Natural random movement
            blob.VelocityX += (_random.NextDouble() - 0.5) * 0.02;
            blob.VelocityY += (_random.NextDouble() - 0.5) * 0.02;

            // Apply damping
            blob.VelocityX *= 0.98;
            blob.VelocityY *= 0.98;

            // Limit velocity
            double maxVelocity = 0.3 * speed;
            blob.VelocityX = Math.Clamp(blob.VelocityX, -maxVelocity, maxVelocity);
            blob.VelocityY = Math.Clamp(blob.VelocityY, -maxVelocity, maxVelocity);

            // Update actual position
            blob.X += blob.VelocityX * speed;
            blob.Y += blob.VelocityY * speed;

            // Wrap around edges
            if (blob.X < -blob.Radius / 10) blob.X = _keyboardService.MaxColumns + blob.Radius / 10;
            if (blob.X > _keyboardService.MaxColumns + blob.Radius / 10) blob.X = -blob.Radius / 10;
            if (blob.Y < -blob.Radius / 10) blob.Y = _keyboardService.MaxRows + blob.Radius / 10;
            if (blob.Y > _keyboardService.MaxRows + blob.Radius / 10) blob.Y = -blob.Radius / 10;
        }

        // Calculate blob offsets based on key attraction (moved outside the pixel rendering loop)
        foreach (var blob in _blobs)
        {
            // Calculate target offset based on pressed keys
            double targetOffsetX = 0;
            double targetOffsetY = 0;

            if (pressedPositions.Count > 0 && attraction > 0)
            {
                double totalWeight = 0;

                foreach (var (keyRow, keyCol, pressure) in pressedPositions)
                {
                    double keyDx = keyCol - blob.X;
                    double keyDy = keyRow - blob.Y;
                    double keyDistance = Math.Sqrt(keyDx * keyDx + keyDy * keyDy);

                    if (keyDistance > 0.1)
                    {
                        // Size factor: bigger blobs are less affected (0.5 to 1.0 range)
                        double sizeRatio = (blob.Radius - minSize) / Math.Max(1, maxSize - minSize);
                        double sizeFactor = 1.0 - (sizeRatio * 0.5); // Large blobs = 0.5x, small blobs = 1.0x
                        
                        // Distance factor: closer keys have exponentially more pull
                        double distanceFactor = 1.0 / (1.0 + keyDistance * keyDistance * 0.05);
                        
                        // Calculate pull strength based on distance, pressure, and size
                        double weight = pressure * attraction * distanceFactor * sizeFactor;
                        
                        // Calculate offset toward this key (partial movement)
                        double offsetAmount = Math.Min(keyDistance, keyDistance * weight * 0.8);
                        targetOffsetX += (keyDx / keyDistance) * offsetAmount * weight;
                        targetOffsetY += (keyDy / keyDistance) * offsetAmount * weight;
                        totalWeight += weight;
                    }
                }

                if (totalWeight > 0)
                {
                    targetOffsetX /= totalWeight;
                    targetOffsetY /= totalWeight;
                }
            }

            // Smoothly interpolate current offset toward target offset
            double smoothFactor = 0.15; // Higher = faster response, lower = smoother
            blob.DisplayOffsetX += (targetOffsetX - blob.DisplayOffsetX) * smoothFactor;
            blob.DisplayOffsetY += (targetOffsetY - blob.DisplayOffsetY) * smoothFactor;
        }

        // Clear buffer
        ClearBuffer();

        // Render blobs with attraction offset
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                double totalIntensity = 0;
                double totalR = 0;
                double totalG = 0;
                double totalB = 0;

                foreach (var blob in _blobs)
                {
                    // Calculate final display position with smoothed offset
                    double displayX = blob.X + blob.DisplayOffsetX;
                    double displayY = blob.Y + blob.DisplayOffsetY;

                    // Calculate influence at this pixel from the display position
                    double dx = col - displayX;
                    double dy = row - displayY;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    // Calculate influence based on distance and radius
                    double normalizedRadius = blob.Radius / 100.0;
                    double influence = Math.Max(0, 1.0 - (distance / (normalizedRadius * 10)));

                    // Apply blur factor (softer edges)
                    influence = Math.Pow(influence, 2.0 - blurFactor);

                    if (influence > 0)
                    {
                        // Interpolate color based on blob size
                        double sizeRatio = (blob.Radius - minSize) / Math.Max(1, maxSize - minSize);
                        var blobColor = EffectUtilities.LerpColor(smallColor, largeColor, sizeRatio);

                        totalR += blobColor.R * influence;
                        totalG += blobColor.G * influence;
                        totalB += blobColor.B * influence;
                        totalIntensity += influence;
                    }
                }

                // Normalize and set color
                if (totalIntensity > 0)
                {
                    byte r = (byte)Math.Clamp(totalR / totalIntensity, 0, 255);
                    byte g = (byte)Math.Clamp(totalG / totalIntensity, 0, 255);
                    byte b = (byte)Math.Clamp(totalB / totalIntensity, 0, 255);

                    // Apply total intensity as brightness multiplier
                    double brightness = Math.Min(1.0, totalIntensity);
                    r = (byte)(r * brightness);
                    g = (byte)(g * brightness);
                    b = (byte)(b * brightness);

                    _colorBuffer[row, col] = new KeyColour(r, g, b);
                }
            }
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    public override void Cleanup()
    {
        _blobs.Clear();
    }

    private class Blob
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public double Radius { get; set; }
        public double DisplayOffsetX { get; set; }
        public double DisplayOffsetY { get; set; }
    }
}
