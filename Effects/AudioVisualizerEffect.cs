using System.Windows.Media;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;
using MediaColor = System.Windows.Media.Color;

namespace WootingRGB.Effects;

public class AudioVisualizerEffect : BaseRGBEffect, IDisposable
{
    private readonly IKeyboardService _keyboardService;
    private WasapiLoopbackCapture? _audioCapture;
    private readonly double[] _audioValues;
    private double[] _columnDecayValues;
    private readonly object _audioLock = new();
    private bool _isCapturing = false;

    public override string Name => "Audio Visualizer";
    public override string Description => "Real-time audio spectrum analyzer visualizer";

    public AudioVisualizerEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
        _audioValues = new double[4800]; // Default buffer size
        _columnDecayValues = Array.Empty<double>();
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "lowColor",
            "Low Frequency Color",
            MediaColor.FromRgb(0x00, 0xFF, 0x00) 
        ));

        _parameters.Add(new ColorParameter(
            "highColor",
            "High Frequency Color",
            MediaColor.FromRgb(0xFF, 0x00, 0x00)
        ));

        _parameters.Add(new ColorParameter(
            "backgroundColor",
            "Background Color",
            MediaColor.FromRgb(0x00, 0x00, 0x00)
        ));

        _parameters.Add(new RangeParameter(
            "sensitivity",
            "Sensitivity",
            EffectParameterType.Intensity,
            defaultValue: 100,
            minValue: 25,
            maxValue: 200
        ));

        _parameters.Add(new RangeParameter(
            "smoothing",
            "Smoothing",
            EffectParameterType.Speed,
            defaultValue: 75,
            minValue: 10,
            maxValue: 100
        ));

        _parameters.Add(new RangeParameter(
            "frequencyRange",
            "Frequency Range (Hz)",
            EffectParameterType.Size,
            defaultValue: 16000,
            minValue: 1000,
            maxValue: 20000
        ));

        _parameters.Add(new BooleanParameter(
            "logarithmicScale",
            "Logarithmic Scale",
            defaultValue: true
        ));
    }

    public override void Initialize()
    {
        base.Initialize();
        _columnDecayValues = new double[_keyboardService.MaxColumns];
        Array.Clear(_columnDecayValues);

        try
        {
            // Use loopback capture to capture system audio
            _audioCapture = new WasapiLoopbackCapture();
            _audioCapture.DataAvailable += OnAudioDataAvailable;
            _audioCapture.StartRecording();
            _isCapturing = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize audio capture: {ex.Message}");
            _isCapturing = false;
        }
    }

    private void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_audioCapture == null) return;

        lock (_audioLock)
        {
            int bytesPerSamplePerChannel = _audioCapture.WaveFormat.BitsPerSample / 8;
            int bytesPerSample = bytesPerSamplePerChannel * _audioCapture.WaveFormat.Channels;
            int bufferSampleCount = e.Buffer.Length / bytesPerSample;

            if (bufferSampleCount > _audioValues.Length)
                bufferSampleCount = _audioValues.Length;

            // Convert audio buffer to double array based on format
            if (bytesPerSamplePerChannel == 2 && _audioCapture.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                for (int i = 0; i < bufferSampleCount; i++)
                    _audioValues[i] = BitConverter.ToInt16(e.Buffer, i * bytesPerSample);
            }
            else if (bytesPerSamplePerChannel == 4 && _audioCapture.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                for (int i = 0; i < bufferSampleCount; i++)
                    _audioValues[i] = BitConverter.ToSingle(e.Buffer, i * bytesPerSample);
            }
        }
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null || !_isCapturing) return;

        var lowColor = GetParameter<ColorParameter>("lowColor")?.ColorValue ?? MediaColor.FromRgb(0, 128, 255);
        var highColor = GetParameter<ColorParameter>("highColor")?.ColorValue ?? MediaColor.FromRgb(255, 0, 128);
        var backgroundColor = GetParameter<ColorParameter>("backgroundColor")?.ColorValue ?? MediaColor.FromRgb(20, 20, 20);
        var sensitivity = GetParameter<RangeParameter>("sensitivity")?.NumericValue ?? 100;
        var smoothing = GetParameter<RangeParameter>("smoothing")?.NumericValue ?? 50;
        var frequencyRange = (int)(GetParameter<RangeParameter>("frequencyRange")?.NumericValue ?? 6000);
        var useLogScale = GetParameter<BooleanParameter>("logarithmicScale")?.BooleanValue ?? true;

        double[] fftMagnitudes;
        
        lock (_audioLock)
        {
            // Perform FFT on audio data
            double[] paddedAudio = FftSharp.Pad.ZeroPad(_audioValues);
            fftMagnitudes = FftSharp.Transform.FFTmagnitude(paddedAudio);
        }

        int fftBinsToUse = Math.Min(frequencyRange * fftMagnitudes.Length / (_audioCapture?.WaveFormat.SampleRate ?? 48000), fftMagnitudes.Length);
        double[] columnValues = SplitAndAverage(fftMagnitudes, fftBinsToUse, _keyboardService.MaxColumns);

        if (useLogScale)
        {
            NormalizeValuesLogarithmic(columnValues);
        }

        // Scale values to fit rows based on sensitivity
        double maxPower = 0.008 * (100.0 / sensitivity);
        ScaleValuesSmooth(columnValues, maxPower, _keyboardService.MaxRows);

        double decaySpeed = (101 - smoothing) / 200.0;
        for (int i = 0; i < _keyboardService.MaxColumns; i++)
        {
            if (columnValues[i] >= _columnDecayValues[i])
            {
                // Instant rise to current value (audio is louder)
                _columnDecayValues[i] = columnValues[i];
            }
            else
            {
                // Smooth falling decay - subtract a consistent amount like gravity
                _columnDecayValues[i] = Math.Max(0, _columnDecayValues[i] - decaySpeed);
                
                if (columnValues[i] > 0 && _columnDecayValues[i] < columnValues[i])
                {
                    _columnDecayValues[i] = columnValues[i];
                }
            }
        }

        // Render visualization with smooth bars
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                // Calculate distance from bottom (inverted row)
                double pixelFromBottom = _keyboardService.MaxRows - row;
                double heightValue = _columnDecayValues[col];
                
                // Only render if height is above a very small threshold
                if (heightValue < 0.01)
                {
                    _colorBuffer[row, col] = new KeyColour(backgroundColor.R, backgroundColor.G, backgroundColor.B);
                }
                else if (pixelFromBottom <= heightValue)
                {
                    // Calculate color gradient from low to high frequency
                    double colorProgress = (double)col / Math.Max(1, _keyboardService.MaxColumns - 1);
                    var columnColor = LerpColor(lowColor, highColor, colorProgress);
                    _colorBuffer[row, col] = new KeyColour(columnColor.R, columnColor.G, columnColor.B);
                }
                else if (pixelFromBottom <= heightValue + 1.0)
                {
                    // Calculate color gradient from low to high frequency
                    double colorProgress = (double)col / Math.Max(1, _keyboardService.MaxColumns - 1);
                    var columnColor = LerpColor(lowColor, highColor, colorProgress);
                    
                    // Smooth anti-aliased edge at the top of the bar
                    double fadeAmount = heightValue + 1.0 - pixelFromBottom;
                    _colorBuffer[row, col] = new KeyColour(
                        (byte)(columnColor.R * fadeAmount),
                        (byte)(columnColor.G * fadeAmount),
                        (byte)(columnColor.B * fadeAmount)
                    );
                }
                else
                {
                    _colorBuffer[row, col] = new KeyColour(backgroundColor.R, backgroundColor.G, backgroundColor.B);
                }
            }
        }

        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }

    private double[] SplitAndAverage(double[] fftMagnitudes, int splitRange, int columns)
    {
        double[] result = new double[columns];
        int range = Math.Max(1, splitRange / columns);

        for (int i = 0; i < columns; i++)
        {
            double highest = 0;
            double sum = 0;
            int count = 0;

            int startIdx = range * i;
            int endIdx = Math.Min(startIdx + range, fftMagnitudes.Length);

            for (int j = startIdx; j < endIdx; j++)
            {
                if (fftMagnitudes[j] > highest)
                    highest = fftMagnitudes[j];
                sum += fftMagnitudes[j];
                count++;
            }

            // Weighted average (75% highest peak, 25% average)
            if (count > 0)
                result[i] = (3 * highest + (sum / count)) / 4;
        }

        return result;
    }

    private void ScaleValuesSmooth(double[] values, double maxPower, int maxRows)
    {
        double scaleFactor = maxRows / maxPower;

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = Math.Min(values[i] * scaleFactor, maxRows);
        }
    }

    private void NormalizeValuesLogarithmic(double[] values)
    {
        int n = values.Length;
        for (int i = 0; i < n; i++)
        {
            // Scaling curve that heavily reduces the very first column (bass)
            // while boosting mids and highs for visibility
            double t = (double)i / (n - 1); // 0 to 1 across columns
            
            // Start at 0.25x for first column, gradually increase to 3.0x for last
            double scaleFactor = 0.25 + Math.Pow(t, 1.2) * 2.75; 
            
            values[i] *= scaleFactor;
        }
    }

    private MediaColor LerpColor(MediaColor color1, MediaColor color2, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return MediaColor.FromRgb(
            (byte)(color1.R + (color2.R - color1.R) * t),
            (byte)(color1.G + (color2.G - color1.G) * t),
            (byte)(color1.B + (color2.B - color1.B) * t)
        );
    }

    public override void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        _isCapturing = false;
        
        if (_audioCapture != null)
        {
            try
            {
                _audioCapture.StopRecording();
                _audioCapture.DataAvailable -= OnAudioDataAvailable;
                _audioCapture.Dispose();
                _audioCapture = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing audio capture: {ex.Message}");
            }
        }

        Array.Clear(_columnDecayValues);
    }
}
