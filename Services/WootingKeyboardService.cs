using System.Diagnostics;
using Wooting;
using static Wooting.WootingKey;

namespace WootingRGB.Services;

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

    public void SetFullKeyboard(KeyColour[,] colors)
    {
        if (!IsInitialized) return;
        RGBControl.SetFull(colors);
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
