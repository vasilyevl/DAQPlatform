using Microsoft.Extensions.Logging;

namespace Grumpy.StatePatternFramework;

/// <summary>Builds and validates a state machine definition.</summary>
public sealed class StateMachineBuilder<TContext>
{
    private readonly Dictionary<Type, IState<TContext>> _states = [];
    private readonly Dictionary<TransitionKey, List<TransitionRule<TContext>>> _transitions = [];

    private Type? _initialStateType;
    private string _name = typeof(TContext).Name;
    private int _queueCapacity = 256;
    private ILogger? _logger;

    /// <summary>Sets the state machine name.</summary>
    public StateMachineBuilder<TContext> Named(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _name = name;
        return this;
    }

    /// <summary>Adds a state instance. Its concrete type is its identity.</summary>
    public StateMachineBuilder<TContext> AddState<TState>(TState state)
        where TState : class, IState<TContext>
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!_states.TryAdd(typeof(TState), state)) {
            throw new InvalidOperationException(
                $"State '{typeof(TState).Name}' is already registered.");
        }

        return this;
    }

    /// <summary>Adds a state using its parameterless constructor.</summary>
    public StateMachineBuilder<TContext> AddState<TState>()
        where TState : class, IState<TContext>, new() =>
        AddState(new TState());

    /// <summary>Sets the initial state.</summary>
    public StateMachineBuilder<TContext> SetInitial<TState>()
        where TState : class, IState<TContext>
    {
        _initialStateType = typeof(TState);
        return this;
    }

    /// <summary>Starts a readable transition declaration.</summary>
    public TransitionBuilder<TContext, TState> On<TState>(StateOutcome outcome)
        where TState : class, IState<TContext> =>
        new(this, outcome);

    /// <summary>Sets the maximum number of pending FSM events.</summary>
    public StateMachineBuilder<TContext> WithQueueCapacity(int capacity)
    {
        if (capacity < 2) {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _queueCapacity = capacity;
        return this;
    }

    /// <summary>Sets the logger used by the FSM.</summary>
    public StateMachineBuilder<TContext> WithLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }

    /// <summary>Validates the definition and creates the state machine.</summary>
    public StateMachine<TContext> Build(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_initialStateType is null) {
            throw new InvalidOperationException("An initial state is required.");
        }

        if (!_states.ContainsKey(_initialStateType)) {
            throw new InvalidOperationException(
                $"Initial state '{_initialStateType.Name}' is not registered.");
        }

        foreach ((TransitionKey key, List<TransitionRule<TContext>> rules) in _transitions) {
            if (!_states.ContainsKey(key.StateType)) {
                throw new InvalidOperationException(
                    $"Transition source '{key.StateType.Name}' is not registered.");
            }

            foreach (TransitionRule<TContext> rule in rules) {
                if (rule.Target is StateTransitionTarget stateTarget &&
                    !_states.ContainsKey(stateTarget.StateType)) {
                    throw new InvalidOperationException(
                        $"Transition target '{stateTarget.StateType.Name}' is not registered.");
                }
            }
        }

        return new StateMachine<TContext>(
            _name,
            context,
            new Dictionary<Type, IState<TContext>>(_states),
            _transitions.ToDictionary(
                item => item.Key,
                item => (IReadOnlyList<TransitionRule<TContext>>)item.Value.ToArray()),
            _initialStateType,
            _queueCapacity,
            _logger);
    }

    internal void AddTransition<TState, TNextState>(
        StateOutcome outcome,
        GuardDefinition<TContext>? guard)
        where TState : class, IState<TContext>
        where TNextState : class, IState<TContext>
    {
        if (outcome.IsStay) {
            throw new ArgumentException(
                "Stay does not require a registered transition.",
                nameof(outcome));
        }

        var key = new TransitionKey(typeof(TState), outcome);
        var rule = new TransitionRule<TContext>(
            key,
            guard,
            new StateTransitionTarget(typeof(TNextState)));

        if (!_transitions.TryGetValue(key, out List<TransitionRule<TContext>>? rules)) {
            _transitions.Add(key, [rule]);
            return;
        }

        if (guard is null || rules.Any(existing => existing.Guard is null)) {
            throw new InvalidOperationException(
                $"Transition '{typeof(TState).Name} / {outcome}' already has " +
                "an unguarded rule. Add guards to all rules for this transition.");
        }

        rules.Add(rule);
    }
}

/// <summary>Completes a fluent transition declaration.</summary>
public sealed class TransitionBuilder<TContext, TState>
    where TState : class, IState<TContext>
{
    private readonly StateMachineBuilder<TContext> _builder;
    private readonly StateOutcome _outcome;
    private readonly GuardDefinition<TContext>? _guard;

    internal TransitionBuilder(
        StateMachineBuilder<TContext> builder,
        StateOutcome outcome,
        GuardDefinition<TContext>? guard = null)
    {
        _builder = builder;
        _outcome = outcome;
        _guard = guard;
    }

    /// <summary>Adds a context-only guard to this transition.</summary>
    public TransitionBuilder<TContext, TState> When(
        Func<TContext, bool> guard) =>
        When("Guard", guard);

    /// <summary>Adds a named context-only guard to this transition.</summary>
    public TransitionBuilder<TContext, TState> When(
        string name,
        Func<TContext, bool> guard)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(guard);

        return new TransitionBuilder<TContext, TState>(
            _builder,
            _outcome,
            new GuardDefinition<TContext>(
                name,
                (context, _) => guard(context)));
    }

    /// <summary>Adds a guard that can inspect the current event.</summary>
    public TransitionBuilder<TContext, TState> When(
        Func<TContext, FsmEvent, bool> guard) =>
        When("Guard", guard);

    /// <summary>Adds a named guard that can inspect the current event.</summary>
    public TransitionBuilder<TContext, TState> When(
        string name,
        Func<TContext, FsmEvent, bool> guard)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(guard);

        return new TransitionBuilder<TContext, TState>(
            _builder,
            _outcome,
            new GuardDefinition<TContext>(name, guard));
    }

    /// <summary>Adds a guard for a specific event type.</summary>
    public TransitionBuilder<TContext, TState> WhenEvent<TEvent>(
        Func<TContext, TEvent, bool> guard)
        where TEvent : FsmEvent =>
        WhenEvent("Guard", guard);

    /// <summary>Adds a named guard for a specific event type.</summary>
    public TransitionBuilder<TContext, TState> WhenEvent<TEvent>(
        string name,
        Func<TContext, TEvent, bool> guard)
        where TEvent : FsmEvent
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(guard);

        return new TransitionBuilder<TContext, TState>(
            _builder,
            _outcome,
            new GuardDefinition<TContext>(
                name,
                (context, input) => input is TEvent typedInput &&
                    guard(context, typedInput)));
    }

    /// <summary>Sets the transition target and returns the FSM builder.</summary>
    public StateMachineBuilder<TContext> GoTo<TNextState>()
        where TNextState : class, IState<TContext>
    {
        _builder.AddTransition<TState, TNextState>(_outcome, _guard);
        return _builder;
    }
}

internal readonly record struct TransitionKey(
    Type StateType,
    StateOutcome Outcome);

internal sealed record GuardDefinition<TContext>(
    string Name,
    Func<TContext, FsmEvent, bool> Predicate);

internal sealed record TransitionRule<TContext>(
    TransitionKey Key,
    GuardDefinition<TContext>? Guard,
    TransitionTarget Target);

internal abstract record TransitionTarget;

internal sealed record StateTransitionTarget(Type StateType) : TransitionTarget;
