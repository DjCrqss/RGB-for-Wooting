using System.Diagnostics;
using Wooting;
using static Wooting.WootingKey;

namespace WootingRGB.Services;

public class WootingKeyboardService : IKeyboardService
{
    private RGBDeviceInfo[]? _availableDevices;
    private int _selectedDeviceIndex = 0;

    public bool IsInitialized { get; private set; }
    public int DeviceCount { get; private set; }
    public int MaxRows { get; private set; }
    public int MaxColumns { get; private set; }
    public RGBDeviceInfo[]? AvailableDevices => _availableDevices;
    public int SelectedDeviceIndex => _selectedDeviceIndex;

    public bool Initialize()
    {
        try
        {
            if (!RGBControl.IsConnected())
                return false;

            var count = RGBControl.GetDeviceCount();
            _availableDevices = new RGBDeviceInfo[count];

            for (byte i = 0; i < count; i++)
            {
                RGBControl.SetControlDevice(i);
                var device = RGBControl.GetDeviceInfo();
                Debug.WriteLine($"Found device {i}: Connected: {device.Connected}, Model: {device.Model}, Type: {device.DeviceType}, Max Rows: {device.MaxRows}, Max Cols: {device.MaxColumns}");
                _availableDevices[i] = device;
            }

            DeviceCount = count;
            
            // Set first device as default
            if (count > 0)
            {
                SetDevice(0);
            }

            IsInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize keyboard: {ex.Message}");
            return false;
        }
    }

    public bool SetDevice(int deviceIndex)
    {
        if (_availableDevices == null || deviceIndex < 0 || deviceIndex >= _availableDevices.Length)
            return false;

        try
        {
            RGBControl.SetControlDevice((byte)deviceIndex);
            _selectedDeviceIndex = deviceIndex;
            
            var device = _availableDevices[deviceIndex];
            MaxColumns = device.MaxColumns;
            MaxRows = device.MaxRows;
            
            Debug.WriteLine($"Switched to device {deviceIndex}: {device.Model}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to set device {deviceIndex}: {ex.Message}");
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
            _availableDevices = null;
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
