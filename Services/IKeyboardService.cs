namespace WootingRGB.Services;

public interface IKeyboardService
{
    bool IsInitialized { get; }
    int DeviceCount { get; }
    int MaxRows { get; }
    int MaxColumns { get; }
    Wooting.RGBDeviceInfo[]? AvailableDevices { get; }
    int SelectedDeviceIndex { get; }

    bool Initialize();
    bool SetDevice(int deviceIndex);
    void Shutdown();
    void SetFullKeyboard(Wooting.KeyColour[,] colors);
    void UpdateKeyboard();
    void ResetKeyboard();
}
