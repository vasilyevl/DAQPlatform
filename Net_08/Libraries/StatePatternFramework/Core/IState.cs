namespace Grumpy.StatePatternFramework;

/// <summary>Defines one state in a state machine.</summary>
public interface IState<TContext>
{
    /// <summary>Called on the FSM thread when this state becomes active.</summary>
    void Enter(TContext context, CancellationToken cancellationToken);

    /// <summary>Handles one event on the FSM thread.</summary>
    StateOutcome Handle(
        TContext context,
        FsmEvent input,
        CancellationToken cancellationToken);

    /// <summary>Called on the FSM thread before leaving this state.</summary>
    void Exit(TContext context, CancellationToken cancellationToken);
}

/// <summary>Provides no-op entry and exit behavior for simple states.</summary>
public abstract class State<TContext> : IState<TContext>
{
    /// <inheritdoc />
    public virtual void Enter(
        TContext context,
        CancellationToken cancellationToken)
    {
    }

    /// <inheritdoc />
    public abstract StateOutcome Handle(
        TContext context,
        FsmEvent input,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public virtual void Exit(
        TContext context,
        CancellationToken cancellationToken)
    {
    }
}
