# Wooting RGB Controller

A modular WPF application for controlling RGB effects on Wooting keyboards.
To run this, execute `dotnet run` or `dotnet build` in the directory in a terminal window.
You also need to include a `wooting-rgb-sdk.dll` inside the /bin/Debug/net9.0-windows folder so that the binary can access the file.
This dll can be [found here](https://github.com/WootingKb/wooting-rgb-sdk/releases/tag/v1.8.0) 




## Adding a New RGB Effect

To create a new RGB effect:

1. **Create a new file** in the `Effects/` folder (e.g., `WaveEffect.cs`)

2. **Inherit from `BaseRGBEffect`**:

```csharp
using System.Windows.Media;
using Wooting;
using WootingRGB.Core;
using WootingRGB.Services;

namespace WootingRGB.Effects;

public class WaveEffect : BaseRGBEffect
{
    private readonly IKeyboardService _keyboardService;
    
    public override string Name => "Wave";
    public override string Description => "Smooth wave pattern";

    public WaveEffect(IKeyboardService keyboardService)
    {
        _keyboardService = keyboardService;
    }

    protected override void InitializeParameters()
    {
        _parameters.Add(new ColorParameter(
            "waveColor",
            "Wave Color",
            Colors.Blue
        ));

        _parameters.Add(new RangeParameter(
            "speed",
            "Wave Speed",
            EffectParameterType.Speed,
            defaultValue: 50,
            minValue: 1,
            maxValue: 100
        ));
    }

    public override void Update(KeyboardState keyboardState)
    {
        if (_colorBuffer == null) return;

        var color = GetParameter<ColorParameter>("waveColor")?.ColorValue ?? Colors.Blue;
        var speed = GetParameter<RangeParameter>("speed")?.NumericValue ?? 50;

        // Use actual keyboard dimensions from the service
        for (int row = 0; row < _keyboardService.MaxRows; row++)
        {
            for (int col = 0; col < _keyboardService.MaxColumns; col++)
            {
                // Your effect logic here
                _colorBuffer[row, col] = new KeyColour(color.R, color.G, color.B);
            }
        }

        // Send to keyboard in one call
        _keyboardService.SetFullKeyboard(_colorBuffer);
        _keyboardService.UpdateKeyboard();
    }
}
```

3. **Register the effect** in `Managers/EffectManager.cs`:

```csharp
_availableEffects.Add(new WaveEffect(_keyboardService));
```

## BaseRGBEffect Helper Methods

### `InitializeColorBuffer()`
Called automatically by base `Initialize()`. Creates `KeyColour[RGBControl.MaxRGBRows, RGBControl.MaxRGBCols]` buffer.

### `SetPixel(int row, int col, byte r, byte g, byte b)`
Helper to set a single key in the buffer (creates KeyColour internally).

### `ClearBuffer()`
Sets all keys in the entire buffer to black (0, 0, 0).

### `_colorBuffer`
Direct access to `KeyColour[RGBControl.MaxRGBRows, RGBControl.MaxRGBCols]` array.

### `_keyboardService.MaxRows` / `_keyboardService.MaxColumns`
**Use these for effect logic** - actual keyboard dimensions.

## Example: Setting All Keys to Red

```csharp
// Use keyboard service dimensions, not buffer dimensions
for (int row = 0; row < _keyboardService.MaxRows; row++)
{
    for (int col = 0; col < _keyboardService.MaxColumns; col++)
    {
        _colorBuffer[row, col] = new KeyColour(255, 0, 0);
    }
}
_keyboardService.SetFullKeyboard(_colorBuffer);
_keyboardService.UpdateKeyboard();
```
