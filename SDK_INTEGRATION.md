# Wooting SDK Integration Guide

## Installing the Wooting Analog SDK

Since the NuGet package isn't available publicly, you'll need to manually add the SDK:

### Option 1: Local DLL Reference
1. Download the Wooting Analog SDK DLL files
2. Place them in a `lib/` folder in your project
3. Add reference in `WootingRGB.csproj`:

```xml
<ItemGroup>
  <Reference Include="WootingAnalogSDK">
    <HintPath>lib\WootingAnalogSDK.dll</HintPath>
  </Reference>
</ItemGroup>
```

### Option 2: Source Code
1. Add the SDK source files directly to your project
2. Create a `SDK/` folder and copy the SDK .cs files

## Updating WootingKeyboardService.cs

Replace the stub implementation with actual SDK calls:

```csharp
using Wooting; // or whatever the actual namespace is

public class WootingKeyboardService : IKeyboardService
{
    private RGBDeviceInfo[] _devices = Array.Empty<RGBDeviceInfo>();

    public bool Initialize()
    {
        try
        {
            if (!RGBControl.IsConnected())
                return false;

            var count = RGBControl.GetDeviceCount();
            DeviceCount = count;
            _devices = new RGBDeviceInfo[count];

            for (byte i = 0; i < count; i++)
            {
                RGBControl.SetControlDevice(i);
                _devices[i] = RGBControl.GetDeviceInfo();
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

    public void SetKeyColor(byte row, byte col, byte r, byte g, byte b)
    {
        if (!IsInitialized) return;
        RGBControl.SetKey(row, col, r, g, b);
    }

    public void SetFullKeyboard(byte[,] colors)
    {
        if (!IsInitialized) return;

        KeyColour[,] keys = new KeyColour[RGBControl.MaxRGBRows, RGBControl.MaxRGBCols];
        
        for (byte row = 0; row < RGBControl.MaxRGBRows; row++)
        {
            for (byte col = 0; col < RGBControl.MaxRGBCols; col++)
            {
                var index = row * 3 + col * 3;
                if (index + 2 < colors.Length)
                {
                    keys[row, col] = new KeyColour(
                        colors[index],     // R
                        colors[index + 1], // G
                        colors[index + 2]  // B
                    );
                }
            }
        }
        
        RGBControl.SetFull(keys);
    }

    public void UpdateKeyboard()
    {
        if (!IsInitialized) return;
        RGBControl.UpdateKeyboard();
    }

    public void ResetKeyboard()
    {
        if (!IsInitialized) return;
        
        for (byte idx = 0; idx < DeviceCount; idx++)
        {
            RGBControl.SetControlDevice(idx);
            RGBControl.ResetRGB();
        }
    }

    public void Shutdown()
    {
        try
        {
            RGBControl.Close();
            IsInitialized = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error shutting down keyboard: {ex.Message}");
        }
    }
}
```

## Updating AnalogInputService.cs

Replace the stub implementation:

```csharp
using Wooting; // or whatever the actual namespace is

public class AnalogInputService
{
    public bool Initialize()
    {
        try
        {
            var (numDevices, error) = WootingAnalogSDK.Initialise();
            _isInitialized = error == WootingAnalogResult.Ok;
            return _isInitialized;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize analog input: {ex.Message}");
            return false;
        }
    }

    public KeyboardState ReadKeyboardState()
    {
        var state = new KeyboardState
        {
            LastUpdate = DateTime.Now
        };

        if (!_isInitialized)
            return state;

        try
        {
            var (keys, readErr) = WootingAnalogSDK.ReadFullBuffer(20);
            if (readErr == WootingAnalogResult.Ok && keys.Count > 0)
            {
                foreach (var analog in keys)
                {
                    // analog.Item1 is the keycode (ushort)
                    // analog.Item2 is the pressure value (float, 0.0 to 1.0)
                    state.PressedKeys[analog.Item1] = analog.Item2;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading keyboard state: {ex.Message}");
        }

        return state;
    }
}
```

## Key Code to Row/Column Mapping

You may need to create a mapping helper to convert key codes to row/column positions:

```csharp
public static class KeyCodeMapper
{
    private static readonly Dictionary<ushort, (byte row, byte col)> _mapping = new()
    {
        // Example mappings - fill with actual Wooting key codes
        { 0x001E, (2, 1) }, // A key
        { 0x0011, (2, 4) }, // W key
        { 0x001F, (3, 1) }, // S key
        { 0x0020, (2, 2) }, // D key
        // Add more mappings as needed
    };

    public static (byte row, byte col)? GetPosition(ushort keyCode)
    {
        return _mapping.TryGetValue(keyCode, out var pos) ? pos : null;
    }
}
```

Then in your reactive effects:

```csharp
foreach (var (keyCode, pressure) in keyboardState.PressedKeys)
{
    var position = KeyCodeMapper.GetPosition(keyCode);
    if (position.HasValue)
    {
        var (row, col) = position.Value;
        // Light up the key at row, col based on pressure
    }
}
```

## Testing Without Hardware

The current stub implementation allows you to test the UI and effect switching without a physical Wooting keyboard connected. Once you integrate the actual SDK, the effects will control the real hardware.

## Performance Considerations

- Effects update at ~60fps (16ms intervals)
- Keep effect calculations efficient to avoid lag
- Use double buffering if needed for complex effects
- The keyboard state is read once per frame and passed to the active effect
