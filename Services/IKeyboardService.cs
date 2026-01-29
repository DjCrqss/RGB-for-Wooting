namespace WootingRGB.Services;

public interface IKeyboardService
{
    bool IsInitialized { get; }
    int DeviceCount { get; }
    
    bool Initialize();
    void Shutdown();
    void SetKeyColor(byte row, byte col, byte r, byte g, byte b);
    void SetFullKeyboard(byte[,] colors);
    void UpdateKeyboard();
    void ResetKeyboard();
}
