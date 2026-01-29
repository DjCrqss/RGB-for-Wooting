using System.Diagnostics;
using Wooting;

namespace WootingRGB.Core;

public abstract class BaseRGBEffect : IRGBEffect
{
    protected List<IEffectParameter> _parameters = new();
    protected DateTime _startTime;
    protected KeyColour[,]? _colorBuffer;
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    public IReadOnlyList<IEffectParameter> Parameters => _parameters.AsReadOnly();

    protected BaseRGBEffect()
    {
        InitializeParameters();
    }

    protected abstract void InitializeParameters();

    public virtual void Initialize()
    {
        _startTime = DateTime.Now;
        InitializeColorBuffer();
    }

    public abstract void Update(KeyboardState keyboardState);

    public virtual void Cleanup()
    {
    }

    protected T? GetParameter<T>(string name) where T : class, IEffectParameter
    {
        return _parameters.FirstOrDefault(p => p.Name == name) as T;
    }

    protected void InitializeColorBuffer()
    {
        _colorBuffer = new KeyColour[RGBControl.MaxRGBRows, RGBControl.MaxRGBCols];
    }

    protected void SetPixel(int row, int col, byte r, byte g, byte b)
    {
        if (_colorBuffer == null) return;
        _colorBuffer[row, col] = new KeyColour(r, g, b);
    }

    protected void ClearBuffer()
    {
        if (_colorBuffer == null) return;
        
        for (int row = 0; row < RGBControl.MaxRGBRows; row++)
        {
            for (int col = 0; col < RGBControl.MaxRGBCols; col++)
            {
                _colorBuffer[row, col] = new KeyColour(0, 0, 0);
            }
        }
    }
}
