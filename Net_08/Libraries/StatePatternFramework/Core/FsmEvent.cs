namespace Grumpy.StatePatternFramework;

/// <summary>Base type for events processed by a state machine.</summary>
public abstract record FsmEvent;

/// <summary>Wraps an application-specific event payload.</summary>
public sealed record DataEvent<T>(T Data) : FsmEvent;

/// <summary>Represents a timer or watchdog notification.</summary>
public sealed record TimerEvent(string TimerId) : FsmEvent;
