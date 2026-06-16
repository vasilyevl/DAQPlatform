namespace Grumpy.StatePatternFramework;

/// <summary>Identifies a registered state.</summary>
public readonly record struct StateId
{
    /// <summary>Initializes a state identifier.</summary>
    public StateId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>Gets the identifier value.</summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;

    internal static StateId FromType(Type stateType) => new(stateType.Name);
}
