using Grumpy.SDAQFramework.Common;

namespace Grumpy.ClickPLCDriver;

/// <summary>Identifies a Click PLC worker operation.</summary>
public enum ClickPlcOperation
{
    Initialize,
    Open,
    Close,
    ReadDiscreteControl,
    WriteDiscreteControl,
    ReadDiscreteControls,
    WriteDiscreteControls,
    ReadInt16Register,
    WriteInt16Register,
    ReadUInt16Register,
    WriteUInt16Register,
    ReadFloat32Register,
    WriteFloat32Register
}

/// <summary>Base class for commands executed by a Click PLC device worker.</summary>
public abstract record ClickPlcCommand(
    Guid Id,
    ClickPlcOperation Operation,
    TimeSpan? ExpectedTimeout = null)
{
    /// <summary>Creates an initialize command from JSON configuration.</summary>
    public static ClickPlcCommand Initialize(
        string configurationJson,
        TimeSpan? expectedTimeout = null) =>
        new InitializeClickPlcCommand(
            Guid.NewGuid(),
            configurationJson,
            expectedTimeout);

    /// <summary>Creates an open command.</summary>
    public static ClickPlcCommand Open(TimeSpan? expectedTimeout = null) =>
        new OpenClickPlcCommand(Guid.NewGuid(), expectedTimeout);

    /// <summary>Creates a close command.</summary>
    public static ClickPlcCommand Close(TimeSpan? expectedTimeout = null) =>
        new CloseClickPlcCommand(Guid.NewGuid(), expectedTimeout);

    /// <summary>Creates a read discrete control command.</summary>
    public static ClickPlcCommand ReadDiscreteControl(
        string name,
        TimeSpan? expectedTimeout = null) =>
        new ReadDiscreteControlCommand(Guid.NewGuid(), name, expectedTimeout);

    /// <summary>Creates a write discrete control command.</summary>
    public static ClickPlcCommand WriteDiscreteControl(
        string name,
        SwitchCtrl state,
        TimeSpan? expectedTimeout = null) =>
        new WriteDiscreteControlCommand(
            Guid.NewGuid(),
            name,
            state,
            expectedTimeout);

    /// <summary>Creates a read discrete controls command.</summary>
    public static ClickPlcCommand ReadDiscreteControls(
        string startName,
        int count,
        TimeSpan? expectedTimeout = null) =>
        new ReadDiscreteControlsCommand(
            Guid.NewGuid(),
            startName,
            count,
            expectedTimeout);

    /// <summary>Creates a write discrete controls command.</summary>
    public static ClickPlcCommand WriteDiscreteControls(
        string startName,
        SwitchCtrl[] states,
        TimeSpan? expectedTimeout = null) =>
        new WriteDiscreteControlsCommand(
            Guid.NewGuid(),
            startName,
            states,
            expectedTimeout);

    /// <summary>Creates a read 16-bit signed register command.</summary>
    public static ClickPlcCommand ReadInt16Register(
        string name,
        TimeSpan? expectedTimeout = null) =>
        new ReadInt16RegisterCommand(Guid.NewGuid(), name, expectedTimeout);

    /// <summary>Creates a write 16-bit signed register command.</summary>
    public static ClickPlcCommand WriteInt16Register(
        string name,
        short value,
        TimeSpan? expectedTimeout = null) =>
        new WriteInt16RegisterCommand(Guid.NewGuid(), name, value, expectedTimeout);

    /// <summary>Creates a read 16-bit unsigned register command.</summary>
    public static ClickPlcCommand ReadUInt16Register(
        string name,
        TimeSpan? expectedTimeout = null) =>
        new ReadUInt16RegisterCommand(Guid.NewGuid(), name, expectedTimeout);

    /// <summary>Creates a write 16-bit unsigned register command.</summary>
    public static ClickPlcCommand WriteUInt16Register(
        string name,
        ushort value,
        TimeSpan? expectedTimeout = null) =>
        new WriteUInt16RegisterCommand(Guid.NewGuid(), name, value, expectedTimeout);

    /// <summary>Creates a read 32-bit floating point register command.</summary>
    public static ClickPlcCommand ReadFloat32Register(
        string name,
        TimeSpan? expectedTimeout = null) =>
        new ReadFloat32RegisterCommand(Guid.NewGuid(), name, expectedTimeout);

    /// <summary>Creates a write 32-bit floating point register command.</summary>
    public static ClickPlcCommand WriteFloat32Register(
        string name,
        float value,
        TimeSpan? expectedTimeout = null) =>
        new WriteFloat32RegisterCommand(Guid.NewGuid(), name, value, expectedTimeout);
}

public sealed record InitializeClickPlcCommand(
    Guid Id,
    string ConfigurationJson,
    TimeSpan? ExpectedTimeout = null)
    : ClickPlcCommand(Id, ClickPlcOperation.Initialize, ExpectedTimeout);

public sealed record OpenClickPlcCommand(
    Guid Id,
    TimeSpan? ExpectedTimeout = null)
    : ClickPlcCommand(Id, ClickPlcOperation.Open, ExpectedTimeout);

public sealed record CloseClickPlcCommand(
    Guid Id,
    TimeSpan? ExpectedTimeout = null)
    : ClickPlcCommand(Id, ClickPlcOperation.Close, ExpectedTimeout);

public sealed record ReadDiscreteControlCommand(
    Guid Id,
    string Name,
    TimeSpan? ExpectedTimeout = null)
    : ClickPlcCommand(Id, ClickPlcOperation.ReadDiscreteControl, ExpectedTimeout);

public sealed record WriteDiscreteControlCommand(
    Guid Id,
    string Name,
    SwitchCtrl State,
    TimeSpan? ExpectedTimeout = null)
    : ClickPlcCommand(Id, ClickPlcOperation.WriteDiscreteControl, ExpectedTimeout);

public sealed record ReadDiscreteControlsCommand(
    Guid Id,
    string StartName,
    int Count,
    TimeSpan? ExpectedTimeout = null)
    : ClickPlcCommand(Id, ClickPlcOperation.ReadDiscreteControls, ExpectedTimeout);

public sealed record WriteDiscreteControlsCommand(
    Guid Id,
    string StartName,
    SwitchCtrl[] States,
    TimeSpan? ExpectedTimeout = null)
    : ClickPlcCommand(Id, ClickPlcOperation.WriteDiscreteControls, ExpectedTimeout);

public sealed record ReadInt16RegisterCommand(
    Guid Id,
    string Name,
    TimeSpan? ExpectedTimeout = null)
    : ClickPlcCommand(Id, ClickPlcOperation.ReadInt16Register, ExpectedTimeout);

public sealed record WriteInt16RegisterCommand(
    Guid Id,
    string Name,
    short Value,
    TimeSpan? ExpectedTimeout = null)
    : ClickPlcCommand(Id, ClickPlcOperation.WriteInt16Register, ExpectedTimeout);

public sealed record ReadUInt16RegisterCommand(
    Guid Id,
    string Name,
    TimeSpan? ExpectedTimeout = null)
    : ClickPlcCommand(Id, ClickPlcOperation.ReadUInt16Register, ExpectedTimeout);

public sealed record WriteUInt16RegisterCommand(
    Guid Id,
    string Name,
    ushort Value,
    TimeSpan? ExpectedTimeout = null)
    : ClickPlcCommand(Id, ClickPlcOperation.WriteUInt16Register, ExpectedTimeout);

public sealed record ReadFloat32RegisterCommand(
    Guid Id,
    string Name,
    TimeSpan? ExpectedTimeout = null)
    : ClickPlcCommand(Id, ClickPlcOperation.ReadFloat32Register, ExpectedTimeout);

public sealed record WriteFloat32RegisterCommand(
    Guid Id,
    string Name,
    float Value,
    TimeSpan? ExpectedTimeout = null)
    : ClickPlcCommand(Id, ClickPlcOperation.WriteFloat32Register, ExpectedTimeout);

/// <summary>Represents the result of a Click PLC worker command.</summary>
public sealed record ClickPlcResult(
    Guid CommandId,
    ClickPlcOperation Operation,
    Results Result,
    object? Value,
    ILogRecord? LastRecord,
    TimeSpan Duration,
    Exception? Exception = null)
{
    /// <summary>Gets whether the operation succeeded.</summary>
    public bool Success =>
        (Result & Results.Success) != 0 &&
        (Result & Results.Error) == 0 &&
        (Result & Results.Cancelled) == 0;
}
