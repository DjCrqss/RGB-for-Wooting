using System.Diagnostics;
using Wooting;
using static Wooting.WootingKey;

namespace WootingRGB.Services;

public class WootingKeyboardService : IKeyboardService
{
    private RGBDeviceInfo[]? _availableDevices;
    private int _selectedDeviceIndex = 0;
    private bool _multiDeviceMode = false;

    public bool IsInitialized { get; private set; }
    public int DeviceCount { get; private set; }
    public int MaxRows { get; private set; }
    public int MaxColumns { get; private set; }
    public RGBDeviceInfo[]? AvailableDevices => _availableDevices;
    public int SelectedDeviceIndex => _selectedDeviceIndex;
    public bool IsMultiDeviceMode => _multiDeviceMode;

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
            _multiDeviceMode = false;
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

    public bool EnableMultiDeviceMode()
    {
        if (_availableDevices == null || DeviceCount <= 0)
            return false;

        try
        {
            _multiDeviceMode = true;
            
            // Use the largest dimensions across all devices
            MaxColumns = 0;
            MaxRows = 0;
            
            foreach (var device in _availableDevices)
            {
                if (device.MaxColumns > MaxColumns)
                    MaxColumns = device.MaxColumns;
                if (device.MaxRows > MaxRows)
                    MaxRows = device.MaxRows;
            }
            
            Debug.WriteLine($"Enabled multi-device mode for {DeviceCount} devices with dimensions {MaxRows}×{MaxColumns}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to enable multi-device mode: {ex.Message}");
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
            _multiDeviceMode = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error shutting down keyboard: {ex.Message}");
        }
    }

    public void SetFullKeyboard(KeyColour[,] colors)
    {
        if (!IsInitialized) return;

        if (_multiDeviceMode && _availableDevices != null)
        {
            // Send to all devices
            for (byte i = 0; i < _availableDevices.Length; i++)
            {
                try
                {
                    RGBControl.SetControlDevice(i);
                    RGBControl.SetFull(colors);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error setting colors on device {i}: {ex.Message}");
                }
            }
        }
        else
        {
            // Single device mode
            RGBControl.SetFull(colors);
        }
    }

    public void UpdateKeyboard()
    {
        if (!IsInitialized) return;

        if (_multiDeviceMode && _availableDevices != null)
        {
            // Update all devices
            for (byte i = 0; i < _availableDevices.Length; i++)
            {
                try
                {
                    RGBControl.SetControlDevice(i);
                    RGBControl.UpdateKeyboard();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating device {i}: {ex.Message}");
                }
            }
        }
        else
        {
            // Single device mode
            RGBControl.UpdateKeyboard();
        }
    }

    public void ResetKeyboard()
    {
        if (!IsInitialized) return;

        if (_multiDeviceMode && _availableDevices != null)
        {
            // Reset all devices
            for (byte i = 0; i < _availableDevices.Length; i++)
            {
                try
                {
                    RGBControl.SetControlDevice(i);
                    RGBControl.ResetRGB();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error resetting device {i}: {ex.Message}");
                }
            }
        }
        else
        {
            // Single device mode
            RGBControl.ResetRGB();
        }
        
        Debug.WriteLine("Reset keyboard to default");
    }
}
