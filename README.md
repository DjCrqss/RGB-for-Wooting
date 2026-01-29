# Wooting RGB Controller

A modular WPF application for controlling RGB effects on Wooting keyboards.

## Project Structure

```
WootingRGB/
??? Core/                           # Core interfaces and base classes
?   ??? IRGBEffect.cs              # Effect interface definition
?   ??? BaseRGBEffect.cs           # Base class for all effects
?   ??? IEffectParameter.cs        # Parameter interface
?   ??? EffectParameters.cs        # Parameter implementations
??? Effects/                        # Individual RGB effect implementations
?   ??? StaticColorEffect.cs       # Solid color effect
?   ??? RainbowEffect.cs           # Rainbow wave effect
?   ??? RainEffect.cs              # Falling rain effect
?   ??? FireEffect.cs              # Flickering fire effect
?   ??? SparklesEffect.cs          # Random sparkles effect
?   ??? ReactiveEffect.cs          # Key press reactive effect
??? Services/                       # SDK integration services
?   ??? IKeyboardService.cs        # Keyboard service interface
?   ??? WootingKeyboardService.cs  # Wooting RGB SDK implementation
?   ??? AnalogInputService.cs      # Wooting Analog SDK implementation
??? Managers/                       # Application managers
?   ??? EffectManager.cs           # Effect lifecycle and switching
??? MainWindow.xaml(.cs)           # Main UI

```

## Adding a New RGB Effect

To create a new RGB effect, follow these steps:

1. **Create a new file** in the `Effects/` folder (e.g., `WaveEffect.cs`)

2. **Inherit from `BaseRGBEffect`**:

```csharp
using System.Windows.Media;
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
        // Add customizable parameters
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
        // Your effect logic here
        // Access parameters using GetParameter<T>("name")
        var color = GetParameter<ColorParameter>("waveColor")?.ColorValue ?? Colors.Blue;
        var speed = GetParameter<RangeParameter>("speed")?.NumericValue ?? 50;

        // Update keyboard colors
        // _keyboardService.SetKeyColor(row, col, r, g, b);
        // _keyboardService.UpdateKeyboard();
    }

    public override void Cleanup()
    {
        // Clean up any state when effect is stopped
        base.Cleanup();
    }
}
```

3. **Register the effect** in `Managers/EffectManager.cs` in the `RegisterAllEffects()` method:

```csharp
_availableEffects.Add(new WaveEffect(_keyboardService));
```

## Parameter Types

### ColorParameter
For RGB color selection. Displays a color picker in the UI.

```csharp
_parameters.Add(new ColorParameter(
    "colorName",
    "Display Name",
    Colors.Red  // default color
));
```

### RangeParameter
For numeric values with min/max ranges. Displays as a slider.

Available types:
- `EffectParameterType.Speed`
- `EffectParameterType.Intensity`
- `EffectParameterType.Size`
- `EffectParameterType.Direction`

```csharp
_parameters.Add(new RangeParameter(
    "paramName",
    "Display Name",
    EffectParameterType.Speed,
    defaultValue: 50,
    minValue: 0,
    maxValue: 100
));
```

## Keyboard State

The `Update()` method receives a `KeyboardState` object containing:
- `PressedKeys`: Dictionary of currently pressed keys with their pressure values
- `LastUpdate`: Timestamp of the last update

## Wooting SDK Integration

### TODO: Replace stub implementations with actual SDK calls

1. **Install the Wooting SDK package** (you'll need to find the correct package name/source)

2. **Update `WootingKeyboardService.cs`**:
   - Replace stub `Initialize()` with `RGBControl` initialization
   - Implement `SetKeyColor()`, `SetFullKeyboard()`, `UpdateKeyboard()`, `ResetKeyboard()`

3. **Update `AnalogInputService.cs`**:
   - Replace stub `Initialize()` with `WootingAnalogSDK.Initialise()`
   - Implement `ReadKeyboardState()` with `WootingAnalogSDK.ReadFullBuffer()`

## Constants

- `MaxRows = 6` - Maximum keyboard rows
- `MaxCols = 21` - Maximum keyboard columns
- Update timer runs at ~60fps (16ms intervals)

## Example: Using Pressed Key Data

```csharp
public override void Update(KeyboardState keyboardState)
{
    foreach (var (keyCode, pressure) in keyboardState.PressedKeys)
    {
        // React to pressed keys
        // Convert keyCode to row/col and light up based on pressure
    }
}
```

## Architecture Benefits

? **Modular**: Each effect is self-contained in its own file  
? **Expandable**: Add new effects without modifying existing code  
? **Customizable**: Effects declare their own parameters automatically displayed in UI  
? **State Management**: Effects maintain their own state between updates  
? **Reactive**: Full access to analog key press data and timing  
? **Clean Separation**: SDK logic separated from effect logic
