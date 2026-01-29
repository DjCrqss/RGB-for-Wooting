using System.Diagnostics;
using WootingRGB.Core;
using WootingRGB.Effects;
using WootingRGB.Services;

namespace WootingRGB.Managers;

public class EffectManager
{
    private readonly IKeyboardService _keyboardService;
    private readonly AnalogInputService _analogInputService;
    private IRGBEffect? _currentEffect;
    private System.Threading.Timer? _updateTimer;
    private readonly List<IRGBEffect> _availableEffects;

    public IRGBEffect? CurrentEffect => _currentEffect;
    public IReadOnlyList<IRGBEffect> AvailableEffects => _availableEffects.AsReadOnly();

    public event EventHandler<IRGBEffect?>? EffectChanged;

    public EffectManager(IKeyboardService keyboardService, AnalogInputService analogInputService)
    {
        _keyboardService = keyboardService;
        _analogInputService = analogInputService;
        _availableEffects = new List<IRGBEffect>();
        
        RegisterAllEffects();
    }

    private void RegisterAllEffects()
    {
        _availableEffects.Add(new StaticColorEffect(_keyboardService));
        _availableEffects.Add(new RainbowEffect(_keyboardService));
        _availableEffects.Add(new RainEffect(_keyboardService));
        _availableEffects.Add(new FireEffect(_keyboardService));
        _availableEffects.Add(new SparklesEffect(_keyboardService));
        _availableEffects.Add(new ReactiveEffect(_keyboardService));
        _availableEffects.Add(new BreathingEffect(_keyboardService));
    }

    public void SetEffect(IRGBEffect effect)
    {
        StopCurrentEffect();

        _currentEffect = effect;
        _currentEffect.Initialize();

        StartUpdateLoop();
        EffectChanged?.Invoke(this, _currentEffect);
    }

    public void StopCurrentEffect()
    {
        _updateTimer?.Dispose();
        _updateTimer = null;

        if (_currentEffect != null)
        {
            _currentEffect.Cleanup();
            _currentEffect = null;
        }

        _keyboardService.ResetKeyboard();
    }

    private void StartUpdateLoop()
    {
        _updateTimer = new System.Threading.Timer(
            callback: _ => UpdateEffect(),
            state: null,
            dueTime: 0,
            period: 16 // ~60fps
        );
    }

    private void UpdateEffect()
    {
        if (_currentEffect == null)
            return;

        var keyboardState = _analogInputService.ReadKeyboardState();
        _currentEffect.Update(keyboardState);
    }

    public void Shutdown()
    {
        StopCurrentEffect();
    }
}
