using System.Collections.Concurrent;
using Grumpy.StatePatternFramework;

namespace StatePatternFrameworkTests;

public sealed class StateMachineTests
{
    [Fact]
    public void StateMachine_TransitionsOnDedicatedThread()
    {
        var context = new TestContext();
        using var transitioned = new ManualResetEventSlim();
        using StateMachine<TestContext> machine =
            new StateMachineBuilder<TestContext>()
                .Named("TransitionTest")
                .AddState(new WaitingState())
                .AddState(new CompleteState())
                .SetInitial<WaitingState>()
                .On<WaitingState>(StateOutcome.Success)
                    .GoTo<CompleteState>()
                .Build(context);

        machine.StateChanged += (_, _) => transitioned.Set();
        machine.Start();

        Assert.True(
            machine.TryPost(new DataEvent<string>("continue"), out string error),
            error);
        Assert.True(transitioned.Wait(TimeSpan.FromSeconds(2)));

        Assert.Equal(nameof(CompleteState), machine.CurrentState?.Value);
        Assert.NotNull(machine.ThreadId);
        Assert.All(
            context.ThreadIds,
            threadId => Assert.Equal(machine.ThreadId, threadId));
    }

    [Fact]
    public void StateMachine_StopsWhileWaitingForInput()
    {
        using StateMachine<TestContext> machine =
            new StateMachineBuilder<TestContext>()
                .AddState(new WaitingState())
                .SetInitial<WaitingState>()
                .Build(new TestContext());

        machine.Start();

        Assert.True(machine.Stop(TimeSpan.FromSeconds(2)));
        Assert.Equal(StateMachineStatus.Stopped, machine.Status);
    }

    [Fact]
    public void StateMachine_FaultsOnMissingTransition()
    {
        using var faulted = new ManualResetEventSlim();
        using StateMachine<TestContext> machine =
            new StateMachineBuilder<TestContext>()
                .AddState(new FailingState())
                .SetInitial<FailingState>()
                .Build(new TestContext());

        machine.Faulted += (_, _) => faulted.Set();
        machine.Start();
        Assert.True(
            machine.TryPost(new DataEvent<int>(1), out string error),
            error);

        Assert.True(faulted.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal(StateMachineStatus.Faulted, machine.Status);
        Assert.IsType<InvalidOperationException>(machine.LastFault);
    }

    [Fact]
    public void Builder_RejectsDuplicateTransitions()
    {
        var builder = new StateMachineBuilder<TestContext>()
            .AddState(new WaitingState())
            .AddState(new CompleteState())
            .SetInitial<WaitingState>()
            .On<WaitingState>(StateOutcome.Success)
                .GoTo<CompleteState>();

        Assert.Throws<InvalidOperationException>(() =>
            builder.On<WaitingState>(StateOutcome.Success)
                .GoTo<CompleteState>());
    }

    [Fact]
    public void StateMachine_SelectsTransitionByContextGuard()
    {
        var context = new TestContext { UseAlternatePath = true };
        using var transitioned = new ManualResetEventSlim();
        using StateMachine<TestContext> machine =
            new StateMachineBuilder<TestContext>()
                .AddState(new WaitingState())
                .AddState(new CompleteState())
                .AddState(new AlternateCompleteState())
                .SetInitial<WaitingState>()
                .On<WaitingState>(StateOutcome.Success)
                    .When("Normal path", ctx => !ctx.UseAlternatePath)
                    .GoTo<CompleteState>()
                .On<WaitingState>(StateOutcome.Success)
                    .When("Alternate path", ctx => ctx.UseAlternatePath)
                    .GoTo<AlternateCompleteState>()
                .Build(context);

        machine.StateChanged += (_, _) => transitioned.Set();
        machine.Start();

        Assert.True(
            machine.TryPost(new DataEvent<string>("continue"), out string error),
            error);
        Assert.True(transitioned.Wait(TimeSpan.FromSeconds(2)));

        Assert.Equal(nameof(AlternateCompleteState), machine.CurrentState?.Value);
    }

    [Fact]
    public void StateMachine_SelectsTransitionByEventGuard()
    {
        using var transitioned = new ManualResetEventSlim();
        using StateMachine<TestContext> machine =
            new StateMachineBuilder<TestContext>()
                .AddState(new EventDrivenState())
                .AddState(new CompleteState())
                .AddState(new AlternateCompleteState())
                .SetInitial<EventDrivenState>()
                .On<EventDrivenState>(StateOutcome.Success)
                    .WhenEvent<DataEvent<string>>(
                        "Expected event",
                        (_, input) => input.Data == "expected")
                    .GoTo<CompleteState>()
                .On<EventDrivenState>(StateOutcome.Success)
                    .WhenEvent<DataEvent<string>>(
                        "Other event",
                        (_, input) => input.Data == "other")
                    .GoTo<AlternateCompleteState>()
                .Build(new TestContext());

        machine.StateChanged += (_, _) => transitioned.Set();
        machine.Start();

        Assert.True(
            machine.TryPost(new DataEvent<string>("expected"), out string error),
            error);
        Assert.True(transitioned.Wait(TimeSpan.FromSeconds(2)));

        Assert.Equal(nameof(CompleteState), machine.CurrentState?.Value);
    }

    [Fact]
    public void Builder_RejectsMixingUnguardedAndGuardedTransitions()
    {
        var builder = new StateMachineBuilder<TestContext>()
            .AddState(new WaitingState())
            .AddState(new CompleteState())
            .AddState(new AlternateCompleteState())
            .SetInitial<WaitingState>()
            .On<WaitingState>(StateOutcome.Success)
                .GoTo<CompleteState>();

        Assert.Throws<InvalidOperationException>(() =>
            builder.On<WaitingState>(StateOutcome.Success)
                .When("Guarded", _ => true)
                .GoTo<AlternateCompleteState>());
    }

    [Fact]
    public void StateMachine_FaultsWhenNoGuardMatches()
    {
        using var faulted = new ManualResetEventSlim();
        using StateMachine<TestContext> machine =
            new StateMachineBuilder<TestContext>()
                .AddState(new WaitingState())
                .AddState(new CompleteState())
                .SetInitial<WaitingState>()
                .On<WaitingState>(StateOutcome.Success)
                    .When("Never", _ => false)
                    .GoTo<CompleteState>()
                .Build(new TestContext());

        machine.Faulted += (_, _) => faulted.Set();
        machine.Start();
        Assert.True(
            machine.TryPost(new DataEvent<string>("continue"), out string error),
            error);

        Assert.True(faulted.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal(StateMachineStatus.Faulted, machine.Status);
        Assert.Contains("No guard matched", machine.LastFault?.Message);
        Assert.Contains("Never", machine.LastFault?.Message);
    }

    [Fact]
    public void StateMachine_FaultsWhenMultipleGuardsMatch()
    {
        using var faulted = new ManualResetEventSlim();
        using StateMachine<TestContext> machine =
            new StateMachineBuilder<TestContext>()
                .AddState(new WaitingState())
                .AddState(new CompleteState())
                .AddState(new AlternateCompleteState())
                .SetInitial<WaitingState>()
                .On<WaitingState>(StateOutcome.Success)
                    .When("First", _ => true)
                    .GoTo<CompleteState>()
                .On<WaitingState>(StateOutcome.Success)
                    .When("Second", _ => true)
                    .GoTo<AlternateCompleteState>()
                .Build(new TestContext());

        machine.Faulted += (_, _) => faulted.Set();
        machine.Start();
        Assert.True(
            machine.TryPost(new DataEvent<string>("continue"), out string error),
            error);

        Assert.True(faulted.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal(StateMachineStatus.Faulted, machine.Status);
        Assert.Contains("Multiple guards matched", machine.LastFault?.Message);
        Assert.Contains("First", machine.LastFault?.Message);
        Assert.Contains("Second", machine.LastFault?.Message);
    }

    private sealed class TestContext
    {
        public ConcurrentQueue<int> ThreadIds { get; } = new();

        public bool UseAlternatePath { get; init; }
    }

    private sealed class WaitingState : State<TestContext>
    {
        public override void Enter(
            TestContext context,
            CancellationToken cancellationToken) =>
            context.ThreadIds.Enqueue(Environment.CurrentManagedThreadId);

        public override StateOutcome Handle(
            TestContext context,
            FsmEvent input,
            CancellationToken cancellationToken)
        {
            context.ThreadIds.Enqueue(Environment.CurrentManagedThreadId);
            return input is DataEvent<string> { Data: "continue" }
                ? StateOutcome.Success
                : StateOutcome.Stay;
        }

        public override void Exit(
            TestContext context,
            CancellationToken cancellationToken) =>
            context.ThreadIds.Enqueue(Environment.CurrentManagedThreadId);
    }

    private sealed class CompleteState : State<TestContext>
    {
        public override void Enter(
            TestContext context,
            CancellationToken cancellationToken) =>
            context.ThreadIds.Enqueue(Environment.CurrentManagedThreadId);

        public override StateOutcome Handle(
            TestContext context,
            FsmEvent input,
            CancellationToken cancellationToken) =>
            StateOutcome.Stay;
    }

    private sealed class AlternateCompleteState : State<TestContext>
    {
        public override StateOutcome Handle(
            TestContext context,
            FsmEvent input,
            CancellationToken cancellationToken) =>
            StateOutcome.Stay;
    }

    private sealed class EventDrivenState : State<TestContext>
    {
        public override StateOutcome Handle(
            TestContext context,
            FsmEvent input,
            CancellationToken cancellationToken) =>
            StateOutcome.Success;
    }

    private sealed class FailingState : State<TestContext>
    {
        public override StateOutcome Handle(
            TestContext context,
            FsmEvent input,
            CancellationToken cancellationToken) =>
            StateOutcome.Error;
    }
}
