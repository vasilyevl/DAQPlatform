namespace Grumpy.StatePatternFramework;

/// <summary>Describes the result produced while handling an FSM event.</summary>
public readonly record struct StateOutcome
{
    /// <summary>Indicates that the current state should remain active.</summary>
    public static StateOutcome Stay { get; } = default;

    /// <summary>Indicates successful processing.</summary>
    public static StateOutcome Success { get; } = new("Success");

    /// <summary>Indicates failed processing.</summary>
    public static StateOutcome Error { get; } = new("Error");

    /// <summary>Indicates that processing timed out.</summary>
    public static StateOutcome Timeout { get; } = new("Timeout");

    /// <summary>Initializes an outcome.</summary>
    public StateOutcome(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>Gets the outcome name.</summary>
    public string? Name { get; }

    /// <summary>Gets whether the FSM should remain in its current state.</summary>
    public bool IsStay => Name is null;

    /// <inheritdoc />
    public override string ToString() => Name ?? nameof(Stay);
}
