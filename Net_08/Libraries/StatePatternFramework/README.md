# State Pattern Framework

The framework runs all state callbacks sequentially on one dedicated thread.
External producers and dedicated device workers communicate with the FSM by
posting events. They must not modify state machine state directly.

## Registration

Concrete state types are their identities, so separate numeric IDs and state
library registration are not required.

```csharp
var machine = new StateMachineBuilder<DeviceContext>()
    .Named("Acquisition")
    .AddState(new InitializeState())
    .AddState(new AcquireState())
    .AddState(new ErrorState())
    .SetInitial<InitializeState>()
    .On<InitializeState>(StateOutcome.Success).GoTo<AcquireState>()
    .On<InitializeState>(StateOutcome.Error).GoTo<ErrorState>()
    .Build(context);
```

The builder rejects duplicate transitions, missing initial states, and
transitions that reference unregistered states.

## Guards

Use guards when the same state and outcome can lead to different targets.
Guards are evaluated on the FSM thread when the transition is selected.

```csharp
builder
    .On<IdleState>(Start)
        .When("Single mode", context => context.Mode == Mode.Single)
        .GoTo<SingleMeasurementState>()
    .On<IdleState>(Start)
        .When("Batch mode", context => context.Mode == Mode.Batch)
        .GoTo<BatchMeasurementState>();
```

Guards can also inspect the current event:

```csharp
builder
    .On<IdleState>(Start)
        .WhenEvent<DataEvent<StartCommand>>(
            "Start command",
            (context, input) => input.Data.IsValid)
        .GoTo<ValidateStartState>();
```

For a given state and outcome, either register one unguarded transition or
register guarded transitions. Mixing guarded and unguarded rules is rejected.
At runtime, exactly one guard must match. No matches or multiple matches fault
the state machine with guard names in the diagnostic message.

## State Contract

```csharp
public sealed class AcquireState : State<DeviceContext>
{
    public override StateOutcome Handle(
        DeviceContext context,
        FsmEvent input,
        CancellationToken cancellationToken)
    {
        return input is DataEvent<DeviceCompleted> completed
            ? StateOutcome.Success
            : StateOutcome.Stay;
    }
}
```

`Enter`, `Handle`, and `Exit` always run on the FSM thread. A state returns
`StateOutcome.Stay` when no transition is required.

## Device Workers

Use `DedicatedReceiver<T>` for a synchronous device-driver pair. It owns one
explicit thread, preserves FIFO ordering, remains alive while idle, and stops
without relying on thread-pool scheduling.

```csharp
var worker = new DedicatedReceiver<DeviceCommand>(
    "CameraWorker",
    capacity: 64,
    (command, cancellationToken) => Execute(command, cancellationToken));

worker.Start();
worker.TrySubmit(command, out string error);
```

Every synchronous device operation still needs a bounded device-level timeout.
Cancellation cannot forcibly interrupt a driver call that never returns.
