using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


[Flags]
[JsonConverter(typeof(StringEnumConverter))]
public enum IOModes
{
    NotSet = 0,
    OnDemand = 1,
    FiniteSamples = 2,
    ContinuousSamples = 4,
    OnDemandFiniteSamples = OnDemand | FiniteSamples,
    OnDemandContinuousSamples = OnDemand | ContinuousSamples,
    FiniteSamplesContinuousSamples = FiniteSamples | ContinuousSamples,
    AllMods = OnDemand | FiniteSamples | ContinuousSamples
}


[JsonObject(MemberSerialization.OptIn)]
public class IOMode : IEquatable<IOMode>, IEquatable<IOModes>
{
    private IOModes _mode;

    public IOMode() {
        Mode = IOModes.NotSet;
    }

    [JsonProperty]
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public IOModes Mode {
        get => _mode;
        set => _mode = value;
    }

    public IOMode(IOModes mode = IOModes.NotSet) {
        Mode = mode;
    }

    public List<IOModes> ToList() {

        var lst = new List<IOModes>();

        if (IsOnDemand) { lst.Add(IOModes.OnDemand); }
        if (IsFiniteSamples) { lst.Add(IOModes.FiniteSamples); }
        if (IsContinuousSamples) { lst.Add(IOModes.ContinuousSamples); }

        return lst;
    }

    public IOModes[] ToArray() => ToList().ToArray();

    public void FromList(List<IOModes> lst) {

        _mode = IOModes.NotSet;
        lst?.ForEach((x) => _mode |= x);
    }


    // Implicit conversion from DAQmxTaskMode to IOMode
    public static implicit operator IOMode(IOModes mode) =>
        new IOMode(mode);

    // Implicit conversion from DAQmxTaskModeWrapper to DAQmxTaskMode
    public static implicit operator IOModes(IOMode wrapper) =>
        wrapper.Mode;

    // Equals method for IEquatable<DAQmxTaskModeWrapper>
    public bool Equals(IOMode? other) =>
        other != null && Mode == other.Mode;

    public override bool Equals(object? obj) =>
        obj is IOMode other && Equals(other);

    public bool Equals(IOModes mode) => Mode == mode;

    public override int GetHashCode() => Mode.GetHashCode();

    public override string ToString() => Mode.ToString();

    // Boolean checks for each IOModes value
    public bool IsNotSet => Mode == IOModes.NotSet;

    public bool IsOnDemand => Mode.HasFlag(IOModes.OnDemand);

    public bool IsFiniteSamples =>
        Mode.HasFlag(IOModes.FiniteSamples);

    public bool IsContinuousSamples =>
        Mode.HasFlag(IOModes.ContinuousSamples);

    public bool IsOnDemandAndFiniteSamples =>
        Mode.HasFlag(IOModes.OnDemandFiniteSamples);

    public bool IsOnDemandAndContinuousSamples =>
        Mode.HasFlag(IOModes.OnDemandContinuousSamples);

    public bool IsFiniteSamplesAndContinuousSamples =>
        Mode.HasFlag(IOModes.FiniteSamplesContinuousSamples);

    public bool IsAllMods => Mode.HasFlag(IOModes.AllMods);
}