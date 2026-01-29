namespace WootingRGB.Core;

public interface IRGBEffect
{
    string Name { get; }
    string Description { get; }
    IReadOnlyList<IEffectParameter> Parameters { get; }
    
    void Initialize();
    void Update(KeyboardState keyboardState);
    void Cleanup();
}

public class KeyboardState
{
    public Dictionary<ushort, float> PressedKeys { get; set; } = new();
    public DateTime LastUpdate { get; set; }
}
