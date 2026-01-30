namespace WootingRGB.Core;

public enum EffectParameterType
{
    Color,
    Speed,
    Intensity,
    Size,
    Direction,
    Boolean
}

public interface IEffectParameter
{
    string Name { get; }
    string DisplayName { get; }
    EffectParameterType ParameterType { get; }
    object Value { get; set; }
    object MinValue { get; }
    object MaxValue { get; }
    object DefaultValue { get; }
}
