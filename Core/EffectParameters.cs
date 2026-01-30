using System.Windows.Media;

namespace WootingRGB.Core;

public class ColorParameter : IEffectParameter
{
    public string Name { get; }
    public string DisplayName { get; }
    public EffectParameterType ParameterType => EffectParameterType.Color;
    
    private Color _value;
    public object Value
    {
        get => _value;
        set => _value = value is Color c ? c : Colors.White;
    }
    
    public object MinValue => Colors.Black;
    public object MaxValue => Colors.White;
    public object DefaultValue { get; }

    public Color ColorValue => _value;

    public ColorParameter(string name, string displayName, Color defaultValue)
    {
        Name = name;
        DisplayName = displayName;
        DefaultValue = defaultValue;
        _value = defaultValue;
    }
}

public class RangeParameter : IEffectParameter
{
    public string Name { get; }
    public string DisplayName { get; }
    public EffectParameterType ParameterType { get; }
    
    private double _value;
    public object Value
    {
        get => _value;
        set => _value = value is double d ? Math.Clamp(d, (double)MinValue, (double)MaxValue) : (double)DefaultValue;
    }
    
    public object MinValue { get; }
    public object MaxValue { get; }
    public object DefaultValue { get; }

    public double NumericValue => _value;

    public RangeParameter(string name, string displayName, EffectParameterType type, double defaultValue, double minValue, double maxValue)
    {
        Name = name;
        DisplayName = displayName;
        ParameterType = type;
        DefaultValue = defaultValue;
        MinValue = minValue;
        MaxValue = maxValue;
        _value = defaultValue;
    }
}

public class ChoiceParameter : IEffectParameter
{
    public string Name { get; }
    public string DisplayName { get; }
    public EffectParameterType ParameterType => EffectParameterType.Direction;
    
    private string _value;
    public object Value
    {
        get => _value;
        set => _value = value is string s && Choices.Contains(s) ? s : (string)DefaultValue;
    }
    
    public object MinValue => Choices.First();
    public object MaxValue => Choices.Last();
    public object DefaultValue { get; }
    
    public IReadOnlyList<string> Choices { get; }
    public string StringValue => _value;

    public ChoiceParameter(string name, string displayName, IEnumerable<string> choices, string defaultValue)
    {
        Name = name;
        DisplayName = displayName;
        Choices = choices.ToList();
        
        if (!Choices.Contains(defaultValue))
            throw new ArgumentException($"Default value '{defaultValue}' is not in the list of choices.");
        
        DefaultValue = defaultValue;
        _value = defaultValue;
    }
}
