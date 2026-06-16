using Grumpy.SDAQFramework.Common;
using Microsoft.Extensions.Logging;

namespace Grumpy.StatePatternFramework;

/// <summary>Represents the lifecycle of a state machine.</summary>
public enum StateMachineStatus
{
    Created,
    Starting,
    Running,
    Stopping,
    Stopped,
    Faulted,
    Disposed
}

/// <summary>Runs registered states sequentially on one dedicated thread.</summary>
public sealed class StateMachine<TContext> : IDisposable
{
    private static readonly TimeSpan DefaultStartTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DefaultStopTimeout = TimeSpan.FromSeconds(10);

    private readonly TContext _context;
    private readonly IReadOnlyDictionary<Type, IState<TContext>> _states;
    private readonly IReadOnlyDictionary<TransitionKey, IReadOnlyList<TransitionRule<TContext>>> _transitions;
    private readonly Type _initialStateType;
    private readonly FIFOBase<FsmEvent> _inputQueue;
    private readonly AutoResetEvent _inputAvailable = new(false);
    private readonly ManualResetEventSlim _started = new(false);
    private readonly CancellationTokenSource _stopSource = new();
    private readonly object _lifecycleLock = new();
    private readonly ILogger? _logger;

    private Thread? _thread;
    private Type? _currentStateType;
    private StateMachineStatus _status = StateMachineStatus.Created;
    private Exception? _lastFault;
    private bool _acceptingEvents;

    internal StateMachine(
        string name,
        TContext context,
        IReadOnlyDictionary<Type, IState<TContext>> states,
        IReadOnlyDictionary<TransitionKey, IReadOnlyList<TransitionRule<TContext>>> transitions,
        Type initialStateType,
        int queueCapacity,
        ILogger? logger)
    {
        Name = name;
        _context = context;
        _states = states;
        _transitions = transitions;
        _initialStateType = initialStateType;
        _inputQueue = new FIFOBase<FsmEvent>(queueCapacity, $"{name}.Input");
        _logger = logger;
    }

    /// <summary>Raised synchronously on the FSM thread after a transition.</summary>
    public event EventHandler<StateChangedEventArgs>? StateChanged;

    /// <summary>Raised synchronously on the FSM thread when execution fails.</summary>
    public event EventHandler<StateMachineFaultedEventArgs>? Faulted;

    /// <summary>Gets the FSM name.</summary>
    public string Name { get; }

    /// <summary>Gets the current lifecycle status.</summary>
    public StateMachineStatus Status {
        get {
            lock (_lifecycleLock) {
                return _status;
            }
        }
    }

    /// <summary>Gets the active state identifier, when available.</summary>
    public StateId? CurrentState {
        get {
            lock (_lifecycleLock) {
                return _currentStateType is null
                    ? null
                    : StateId.FromType(_currentStateType);
            }
        }
    }

    /// <summary>Gets the dedicated FSM thread ID, when started.</summary>
    public int? ThreadId => _thread?.ManagedThreadId;

    /// <summary>Gets the last unhandled execution failure.</summary>
    public Exception? LastFault {
        get {
            lock (_lifecycleLock) {
                return _lastFault;
            }
        }
    }

    /// <summary>Starts the dedicated FSM thread and waits until ready.</summary>
    public void Start(TimeSpan? timeout = null)
    {
        lock (_lifecycleLock) {
            EnsureStatus(StateMachineStatus.Created);
            _status = StateMachineStatus.Starting;
            _acceptingEvents = true;
            _thread = new Thread(Run) {
                IsBackground = true,
                Name = Name
            };
            _thread.Start();
        }

        TimeSpan startTimeout = timeout ?? DefaultStartTimeout;
        if (!_started.Wait(startTimeout)) {
            Stop(DefaultStopTimeout);
            throw new TimeoutException(
                $"State machine '{Name}' did not start within {startTimeout}.");
        }

        if (Status == StateMachineStatus.Faulted) {
            throw new InvalidOperationException(
                $"State machine '{Name}' faulted during startup.",
                LastFault);
        }
    }

    /// <summary>Adds an event without waiting for queue space.</summary>
    public bool TryPost(FsmEvent input, out string error)
    {
        ArgumentNullException.ThrowIfNull(input);

        lock (_lifecycleLock) {
            if (!_acceptingEvents) {
                error = $"State machine '{Name}' is not accepting events.";
                return false;
            }

            if (!_inputQueue.Push(input, out error)) {
                return false;
            }
        }

        _inputAvailable.Set();
        return true;
    }

    /// <summary>Requests shutdown and waits for the FSM thread.</summary>
    public bool Stop(TimeSpan? timeout = null)
    {
        Thread? thread;

        lock (_lifecycleLock) {
            if (_status is StateMachineStatus.Stopped or
                StateMachineStatus.Disposed) {
                return true;
            }

            _acceptingEvents = false;
            if (_status != StateMachineStatus.Faulted) {
                _status = StateMachineStatus.Stopping;
            }

            thread = _thread;
            _stopSource.Cancel();
            _inputAvailable.Set();
        }

        if (thread is null) {
            lock (_lifecycleLock) {
                _status = StateMachineStatus.Stopped;
            }
            return true;
        }

        if (thread == Thread.CurrentThread) {
            return false;
        }

        bool stopped = thread.Join(timeout ?? DefaultStopTimeout);
        if (stopped) {
            lock (_lifecycleLock) {
                _thread = null;
                if (_status != StateMachineStatus.Faulted) {
                    _status = StateMachineStatus.Stopped;
                }
            }
        }

        return stopped;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lifecycleLock) {
            if (_status == StateMachineStatus.Disposed) {
                return;
            }
        }

        if (!Stop(DefaultStopTimeout)) {
            throw new TimeoutException(
                $"State machine '{Name}' did not stop within " +
                $"{DefaultStopTimeout}.");
        }

        lock (_lifecycleLock) {
            _status = StateMachineStatus.Disposed;
            _inputQueue.Dispose();
            _inputAvailable.Dispose();
            _started.Dispose();
            _stopSource.Dispose();
        }
    }

    private void Run()
    {
        CancellationToken cancellationToken = _stopSource.Token;
        IState<TContext>? currentState = null;
        bool currentStateEntered = false;

        try {
            currentState = _states[_initialStateType];
            SetCurrentState(_initialStateType);
            currentState.Enter(_context, cancellationToken);
            currentStateEntered = true;

            lock (_lifecycleLock) {
                _status = StateMachineStatus.Running;
            }
            _started.Set();

            WaitHandle[] waitHandles = [
                _inputAvailable,
                cancellationToken.WaitHandle
            ];

            while (!cancellationToken.IsCancellationRequested) {
                int signal = WaitHandle.WaitAny(waitHandles);
                if (signal == 1) {
                    break;
                }

                while (!cancellationToken.IsCancellationRequested &&
                       _inputQueue.Pop(out FsmEvent input, out _)) {
                    StateOutcome outcome = currentState.Handle(
                        _context,
                        input,
                        cancellationToken);

                    if (!outcome.IsStay) {
                        currentState = Transition(
                            currentState,
                            input,
                            outcome,
                            cancellationToken,
                            ref currentStateEntered);
                    }
                }
            }
        }
        catch (Exception exception) {
            RecordFault(exception);
        }
        finally {
            if (currentState is not null && currentStateEntered) {
                try {
                    currentState.Exit(_context, cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested) {
                }
                catch (Exception exception) {
                    RecordFault(exception);
                }
            }

            lock (_lifecycleLock) {
                _acceptingEvents = false;
                if (_status != StateMachineStatus.Faulted) {
                    _status = StateMachineStatus.Stopped;
                }
            }
            _started.Set();
        }
    }

    private IState<TContext> Transition(
        IState<TContext> currentState,
        FsmEvent input,
        StateOutcome outcome,
        CancellationToken cancellationToken,
        ref bool currentStateEntered)
    {
        Type currentType = currentState.GetType();
        var key = new TransitionKey(currentType, outcome);

        if (!_transitions.TryGetValue(
                key,
                out IReadOnlyList<TransitionRule<TContext>>? rules)) {
            throw new InvalidOperationException(
                $"No transition is registered for " +
                $"'{currentType.Name} / {outcome}'.");
        }

        TransitionRule<TContext> rule = SelectTransitionRule(key, rules, input);

        currentState.Exit(_context, cancellationToken);
        currentStateEntered = false;
        Type nextType = GetStateTarget(rule);
        IState<TContext> nextState = _states[nextType];
        nextState.Enter(_context, cancellationToken);
        currentStateEntered = true;
        SetCurrentState(nextType);

        RaiseStateChanged(currentType, nextType, outcome);

        return nextState;
    }

    private TransitionRule<TContext> SelectTransitionRule(
        TransitionKey key,
        IReadOnlyList<TransitionRule<TContext>> rules,
        FsmEvent input)
    {
        if (rules.Count == 1 && rules[0].Guard is null) {
            return rules[0];
        }

        var matchedRules = new List<TransitionRule<TContext>>();
        var failedGuards = new List<string>();

        foreach (TransitionRule<TContext> rule in rules) {
            if (rule.Guard is null) {
                matchedRules.Add(rule);
                continue;
            }

            bool matched;
            try {
                matched = rule.Guard.Predicate(_context, input);
            }
            catch (Exception exception) {
                throw new InvalidOperationException(
                    $"Transition guard '{rule.Guard.Name}' failed for " +
                    $"'{key.StateType.Name} / {key.Outcome}'.",
                    exception);
            }

            if (matched) {
                matchedRules.Add(rule);
            }
            else {
                failedGuards.Add(rule.Guard.Name);
            }
        }

        return matchedRules.Count switch {
            1 => matchedRules[0],
            0 => throw new InvalidOperationException(
                $"No guard matched for transition '{key.StateType.Name} / " +
                $"{key.Outcome}'. Failed guards: {string.Join(", ", failedGuards)}."),
            _ => throw new InvalidOperationException(
                $"Multiple guards matched for transition '{key.StateType.Name} / " +
                $"{key.Outcome}'. Matching guards: " +
                $"{string.Join(", ", matchedRules.Select(GetGuardName))}.")
        };
    }

    private static string GetGuardName(TransitionRule<TContext> rule) =>
        rule.Guard?.Name ?? "Unguarded";

    private static Type GetStateTarget(TransitionRule<TContext> rule) =>
        rule.Target switch {
            StateTransitionTarget stateTarget => stateTarget.StateType,
            _ => throw new NotSupportedException(
                $"Transition target '{rule.Target.GetType().Name}' is not supported.")
        };

    private void RaiseStateChanged(
        Type previousState,
        Type currentState,
        StateOutcome outcome)
    {
        EventHandler<StateChangedEventArgs>? handlers = StateChanged;
        if (handlers is null) {
            return;
        }

        var eventArgs = new StateChangedEventArgs(
            StateId.FromType(previousState),
            StateId.FromType(currentState),
            outcome);

        foreach (EventHandler<StateChangedEventArgs> handler in
                 handlers.GetInvocationList()) {
            try {
                handler(this, eventArgs);
            }
            catch (Exception notificationException) {
                _logger?.LogError(
                    notificationException,
                    "A state change subscriber failed for state machine " +
                    "{StateMachineName}.",
                    Name);
            }
        }
    }

    private void SetCurrentState(Type stateType)
    {
        lock (_lifecycleLock) {
            _currentStateType = stateType;
        }
    }

    private void RecordFault(Exception exception)
    {
        lock (_lifecycleLock) {
            if (_status == StateMachineStatus.Faulted) {
                return;
            }

            _lastFault = exception;
            _status = StateMachineStatus.Faulted;
            _acceptingEvents = false;
        }

        _logger?.LogError(
            exception,
            "State machine {StateMachineName} faulted.",
            Name);
        RaiseFaulted(exception);
    }

    private void RaiseFaulted(Exception exception)
    {
        EventHandler<StateMachineFaultedEventArgs>? handlers = Faulted;
        if (handlers is null) {
            return;
        }

        var eventArgs = new StateMachineFaultedEventArgs(exception);
        foreach (EventHandler<StateMachineFaultedEventArgs> handler in
                 handlers.GetInvocationList()) {
            try {
                handler(this, eventArgs);
            }
            catch (Exception notificationException) {
                _logger?.LogError(
                    notificationException,
                    "A fault subscriber failed for state machine {StateMachineName}.",
                    Name);
            }
        }
    }

    private void EnsureStatus(StateMachineStatus expected)
    {
        if (_status != expected) {
            throw new InvalidOperationException(
                $"State machine '{Name}' is {_status}; expected {expected}.");
        }
    }
}

/// <summary>Provides details about a completed state transition.</summary>
public sealed class StateChangedEventArgs : EventArgs
{
    /// <summary>Initializes the event data.</summary>
    public StateChangedEventArgs(
        StateId previousState,
        StateId currentState,
        StateOutcome outcome)
    {
        PreviousState = previousState;
        CurrentState = currentState;
        Outcome = outcome;
    }

    /// <summary>Gets the state that was exited.</summary>
    public StateId PreviousState { get; }

    /// <summary>Gets the state that was entered.</summary>
    public StateId CurrentState { get; }

    /// <summary>Gets the outcome that selected the transition.</summary>
    public StateOutcome Outcome { get; }
}

/// <summary>Provides details about an FSM execution failure.</summary>
public sealed class StateMachineFaultedEventArgs : EventArgs
{
    /// <summary>Initializes the event data.</summary>
    public StateMachineFaultedEventArgs(Exception exception) =>
        Exception = exception;

    /// <summary>Gets the execution failure.</summary>
    public Exception Exception { get; }
}
