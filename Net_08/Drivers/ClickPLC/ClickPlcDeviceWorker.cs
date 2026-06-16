using System.Diagnostics;
using Grumpy.SDAQFramework.Common;

namespace Grumpy.ClickPLCDriver;

/// <summary>
/// Serializes all Click PLC driver operations on one dedicated worker thread.
/// </summary>
public sealed class ClickPlcDeviceWorker : IDisposable
{
    private static readonly TimeSpan DefaultStopTimeout = TimeSpan.FromSeconds(10);

    private readonly IClickPLCHandler _handler;
    private readonly Action<ClickPlcResult> _completionHandler;
    private readonly DedicatedReceiver<ClickPlcCommand> _receiver;

    /// <summary>Initializes the worker around an existing handler.</summary>
    public ClickPlcDeviceWorker(
        string name,
        IClickPLCHandler handler,
        Action<ClickPlcResult> completionHandler,
        int queueCapacity = 128)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(completionHandler);

        Name = name;
        _handler = handler;
        _completionHandler = completionHandler;
        _receiver = new DedicatedReceiver<ClickPlcCommand>(
            $"{name}.Worker",
            queueCapacity,
            ExecuteCommand);
        _receiver.Faulted += OnReceiverFaulted;
    }

    /// <summary>Gets the worker name.</summary>
    public string Name { get; }

    /// <summary>Gets whether the worker thread is running.</summary>
    public bool IsRunning => _receiver.IsRunning;

    /// <summary>Gets the number of queued commands.</summary>
    public int PendingCount => _receiver.PendingCount;

    /// <summary>Gets the worker thread ID, when started.</summary>
    public int? ThreadId => _receiver.ThreadId;

    /// <summary>Starts the dedicated worker thread.</summary>
    public void Start(TimeSpan? timeout = null) => _receiver.Start(timeout);

    /// <summary>Submits a command to be executed on the worker thread.</summary>
    public bool TrySubmit(ClickPlcCommand command, out string error) =>
        _receiver.TrySubmit(command, out error);

    /// <summary>Requests worker shutdown and waits for the worker thread.</summary>
    public bool Stop(TimeSpan? timeout = null) =>
        _receiver.Stop(timeout ?? DefaultStopTimeout);

    /// <inheritdoc />
    public void Dispose()
    {
        Stop(DefaultStopTimeout);
        _receiver.Dispose();
    }

    private void ExecuteCommand(
        ClickPlcCommand command,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) {
            Publish(command, Results.Cancelled, null, TimeSpan.Zero);
            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        try {
            (bool success, object? value) = ExecuteDriverCall(command);
            stopwatch.Stop();

            Results result = success ? Results.Success : Results.Error;
            if (success &&
                command.ExpectedTimeout is not null &&
                stopwatch.Elapsed > command.ExpectedTimeout.Value) {
                result = Results.Success | Results.Warning | Results.Timeout;
            }

            Publish(command, result, value, stopwatch.Elapsed);
        }
        catch (Exception exception) {
            stopwatch.Stop();
            Publish(
                command,
                Results.Error,
                null,
                stopwatch.Elapsed,
                exception);
        }
    }

    private (bool Success, object? Value) ExecuteDriverCall(ClickPlcCommand command) =>
        command switch {
            InitializeClickPlcCommand init =>
                (_handler.Init(init.ConfigurationJson), null),

            OpenClickPlcCommand =>
                (_handler.Open(), null),

            CloseClickPlcCommand =>
                (_handler.Close(), null),

            ReadDiscreteControlCommand read =>
                ReadDiscreteControl(read.Name),

            WriteDiscreteControlCommand write =>
                (_handler.WriteDiscreteControl(write.Name, write.State), null),

            ReadDiscreteControlsCommand read =>
                ReadDiscreteControls(read.StartName, read.Count),

            WriteDiscreteControlsCommand write =>
                (_handler.WriteDiscreteControls(write.StartName, write.States), null),

            ReadInt16RegisterCommand read =>
                ReadInt16Register(read.Name),

            WriteInt16RegisterCommand write =>
                (_handler.WriteInt16Register(write.Name, write.Value), null),

            ReadUInt16RegisterCommand read =>
                ReadUInt16Register(read.Name),

            WriteUInt16RegisterCommand write =>
                (_handler.WriteUInt16Register(write.Name, write.Value), null),

            ReadFloat32RegisterCommand read =>
                ReadFloat32Register(read.Name),

            WriteFloat32RegisterCommand write =>
                (_handler.WriteFloat32Register(write.Name, write.Value), null),

            _ => throw new NotSupportedException(
                $"Command '{command.GetType().Name}' is not supported.")
        };

    private (bool Success, object? Value) ReadDiscreteControl(string name)
    {
        bool success = _handler.ReadDiscreteControl(name, out SwitchState state);
        return (success, state);
    }

    private (bool Success, object? Value) ReadDiscreteControls(
        string startName,
        int count)
    {
        bool success = _handler.ReadDiscreteControls(
            startName,
            count,
            out SwitchState[] states);
        return (success, states);
    }

    private (bool Success, object? Value) ReadInt16Register(string name)
    {
        bool success = _handler.ReadInt16Register(name, out short value);
        return (success, value);
    }

    private (bool Success, object? Value) ReadUInt16Register(string name)
    {
        bool success = _handler.ReadUInt16Register(name, out ushort value);
        return (success, value);
    }

    private (bool Success, object? Value) ReadFloat32Register(string name)
    {
        bool success = _handler.ReadFloat32Register(name, out float value);
        return (success, value);
    }

    private void Publish(
        ClickPlcCommand command,
        Results result,
        object? value,
        TimeSpan duration,
        Exception? exception = null)
    {
        _completionHandler(
            new ClickPlcResult(
                command.Id,
                command.Operation,
                result,
                value,
                _handler.LastRecord,
                duration,
                exception));
    }

    private void OnReceiverFaulted(
        object? sender,
        ReceiverFaultedEventArgs<ClickPlcCommand> args)
    {
        Publish(
            args.Item,
            Results.Error,
            null,
            TimeSpan.Zero,
            args.Exception);
    }
}
