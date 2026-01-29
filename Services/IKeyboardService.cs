namespace WootingRGB.Services;

public interface IKeyboardService
{
    bool IsInitialized { get; }
    int DeviceCount { get; }
    int MaxRows { get; }
    int MaxColumns { get; }

    bool Initialize();
    void Shutdown();
    void SetFullKeyboard(Wooting.KeyColour[,] colors);
    void UpdateKeyboard();
    void ResetKeyboard();
}
