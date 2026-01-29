using System.Diagnostics;
using Wooting;
using static Wooting.WootingKey;

namespace WootingRGB.Services;

// This is a stub implementation. Replace with actual Wooting SDK calls
// once you have the WootingAnalogSDK.NET package installed
public class WootingKeyboardService : IKeyboardService
{
    public bool IsInitialized { get; private set; }
    public int DeviceCount { get; private set; }

    public int MaxRows { get; private set; }

    public int MaxColumns { get; private set; }

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
                MaxColumns = device.MaxColumns;
                MaxRows = device.MaxRows;
                infos[i] = device;
            }

            DeviceCount = count;
            IsInitialized = true;

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

        KeyColour[,] keys = new KeyColour[RGBControl.MaxRGBRows, RGBControl.MaxRGBCols];
        
        // Initialize all keys to black (off)
        for (byte row = 0; row < RGBControl.MaxRGBRows; row++)
        {
            for (byte col = 0; col < RGBControl.MaxRGBCols; col++)
            {
                keys[row, col] = new KeyColour(0, 0, 0);
            }
        }

        // Fill in colors from the provided array
        // Expected format: colors[row, col] where each element contains RGB packed or
        // colors has dimensions [rows, cols*3] where triplets are R,G,B
        int rows = Math.Min(colors.GetLength(0), RGBControl.MaxRGBRows);
        int cols = colors.GetLength(1);

        if (cols == RGBControl.MaxRGBCols * 3)
        {
            // Format: [row, col*3] where each row contains R,G,B,R,G,B,...
            for (byte row = 0; row < rows; row++)
            {
                for (byte col = 0; col < Math.Min(cols / 3, RGBControl.MaxRGBCols); col++)
                {
                    byte r = colors[row, col * 3];
                    byte g = colors[row, col * 3 + 1];
                    byte b = colors[row, col * 3 + 2];
                    keys[row, col] = new KeyColour(r, g, b);
                }
            }
        }
        else
        {
            // Assume simple format where caller manages the structure
            for (byte row = 0; row < rows; row++)
            {
                for (byte col = 0; col < Math.Min(cols, RGBControl.MaxRGBCols); col++)
                {
                    // If it's a single byte per position, treat as intensity
                    byte value = colors[row, col];
                    keys[row, col] = new KeyColour(value, value, value);
                }
            }
        }

        RGBControl.SetFull(keys);
        Debug.WriteLine("Set full keyboard colors");
    }

    public void UpdateKeyboard()
    {
        if (!IsInitialized) return;
        
        RGBControl.UpdateKeyboard();
        //Debug.WriteLine("Keyboard is updated");
    }

    public void ResetKeyboard()
    {
        if (!IsInitialized) return;
        
        RGBControl.ResetRGB();
        Debug.WriteLine("Reset keyboard to default");
    }
}
