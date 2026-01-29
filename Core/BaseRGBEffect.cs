namespace WootingRGB.Core;

public abstract class BaseRGBEffect : IRGBEffect
{
    protected List<IEffectParameter> _parameters = new();
    protected DateTime _startTime;
    
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
    }

    public abstract void Update(KeyboardState keyboardState);

    public virtual void Cleanup()
    {
    }

    protected T? GetParameter<T>(string name) where T : class, IEffectParameter
    {
        return _parameters.FirstOrDefault(p => p.Name == name) as T;
    }
}
