using System.Diagnostics;
using Wooting;

namespace WootingRGB.Services;

// This is a stub implementation. Replace with actual Wooting SDK calls
// once you have the WootingAnalogSDK.NET package installed
public class WootingKeyboardService : IKeyboardService
{
    public bool IsInitialized { get; private set; }
    public int DeviceCount { get; private set; }

    private const int MaxRows = 6;
    private const int MaxCols = 21;

    public bool Initialize()
    {
        try
        {
            // TODO: Replace with actual SDK initialization
            //var (numDevices, error) = WootingAnalogSDK.Initialise();
            //if (error == WootingAnalogResult.Ok)
            //{
            //    DeviceCount = numDevices;
            //    var count = RGBControl.GetDeviceCount();
            //    IsInitialized = RGBControl.IsConnected();
            //    return IsInitialized;
            //}

            //Debug.WriteLine("Keyboard service initialized (stub mode)");
            //DeviceCount = 1; // Simulated device
            //IsInitialized = true;
            //return true;

            if (!RGBControl.IsConnected())
                return false;

            var count = RGBControl.GetDeviceCount();
            var infos = new RGBDeviceInfo[count];
            for (byte i = 0; i < count; i++)
            {
                RGBControl.SetControlDevice(i);
                var device = RGBControl.GetDeviceInfo();
                Debug.WriteLine($"Found device: Connected: {device.Connected}, Model: {device.Model}, Type: {device.DeviceType}, Max Rows: {device.MaxRows}, Max Cols: {device.MaxColumns}, Max Keycode: {device.KeycodeLimit}");
                infos[i] = device;
            }
            DeviceCount = count;

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize keyboard: {ex.Message}");
            return false;
        }
    }

    public void Shutdown()
    {
        try
        {
            RGBControl.Close();
            Debug.WriteLine("Keyboard service shutdown");
            IsInitialized = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error shutting down keyboard: {ex.Message}");
        }
    }

    public void SetKeyColor(byte row, byte col, byte r, byte g, byte b)
    {
        if (!IsInitialized) return;
        
        RGBControl.SetKey(row, col, r, g, b);
        Debug.WriteLine($"Set key [{row},{col}] to RGB({r},{g},{b})");
    }

    public void SetFullKeyboard(byte[,] colors)
    {
        if (!IsInitialized) return;

        // TODO: Convert byte array to KeyColour array and call RGBControl.SetFull(keys);
        Debug.WriteLine("Set full keyboard colors");
    }

    public void UpdateKeyboard()
    {
        if (!IsInitialized) return;
        
        RGBControl.UpdateKeyboard();
    }

    public void ResetKeyboard()
    {
        if (!IsInitialized) return;
        
        RGBControl.ResetRGB();
        Debug.WriteLine("Reset keyboard to default");
    }
}
