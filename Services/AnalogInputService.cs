using System.Diagnostics;
using WootingAnalogSDKNET;
using WootingRGB.Core;

namespace WootingRGB.Services;

public class AnalogInputService
{
    private bool _isInitialized;

    public bool Initialize()
    {
        try
        {
            var (numDevices, error) = WootingAnalogSDK.Initialise();
            _isInitialized = error == WootingAnalogResult.Ok;

            Debug.WriteLine("Analog input service initialized (stub mode)");
            _isInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize analog input: {ex.Message}");
            return false;
        }
    }

    public KeyboardState ReadKeyboardState()
    {
        var state = new KeyboardState
        {
            LastUpdate = DateTime.Now
        };

        if (!_isInitialized)
            return state;

        try
        {
            var (keys, readErr) = WootingAnalogSDK.ReadFullBuffer(20);
            if (readErr == WootingAnalogResult.Ok && keys.Count > 0)
            {
                foreach (var analog in keys)
                {
                    state.PressedKeys[analog.Item1] = analog.Item2;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading keyboard state: {ex.Message}");
        }

        return state;
    }

    public void Shutdown()
    {
        _isInitialized = false;
        Debug.WriteLine("Analog input service shutdown");
    }
}
