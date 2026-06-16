using System.Collections.Concurrent;
using Grumpy.ClickPLCDriver;
using Grumpy.SDAQFramework.Common;

namespace ClickPlcUnitTest;

public sealed class ClickPlcDeviceWorkerTests
{
    [Fact]
    public void Worker_ExecutesCommandsSequentiallyOnDedicatedThread()
    {
        var handler = new FakeClickPlcHandler();
        var results = new ConcurrentQueue<ClickPlcResult>();
        using var completed = new CountdownEvent(3);
        using var worker = new ClickPlcDeviceWorker(
            "ClickWorkerTest",
            handler,
            result => {
                results.Enqueue(result);
                completed.Signal();
            });

        worker.Start();

        Assert.True(
            worker.TrySubmit(ClickPlcCommand.Open(), out string openError),
            openError);
        Assert.True(
            worker.TrySubmit(
                ClickPlcCommand.WriteFloat32Register("DF1", 12.5f),
                out string writeError),
            writeError);
        Assert.True(
            worker.TrySubmit(
                ClickPlcCommand.ReadFloat32Register("DF1"),
                out string readError),
            readError);

        Assert.True(completed.Wait(TimeSpan.FromSeconds(2)));

        Assert.Equal(3, results.Count);
        Assert.All(results, result => Assert.True(result.Success));
        Assert.Single(handler.ThreadIds.Distinct());
        Assert.Equal(worker.ThreadId, handler.ThreadIds.First());

        ClickPlcResult readResult = results.Last();
        Assert.Equal(ClickPlcOperation.ReadFloat32Register, readResult.Operation);
        Assert.Equal(12.5f, Assert.IsType<float>(readResult.Value));
    }

    [Fact]
    public void Worker_ReportsFailureResult()
    {
        var handler = new FakeClickPlcHandler {
            FailNextWrite = true
        };
        using var completed = new ManualResetEventSlim();
        ClickPlcResult? result = null;
        using var worker = new ClickPlcDeviceWorker(
            "ClickWorkerFailureTest",
            handler,
            completion => {
                result = completion;
                completed.Set();
            });

        worker.Start();

        Assert.True(
            worker.TrySubmit(
                ClickPlcCommand.WriteUInt16Register("DS1", 42),
                out string error),
            error);

        Assert.True(completed.Wait(TimeSpan.FromSeconds(2)));
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(Results.Error, result.Result);
        Assert.Equal(ClickPlcOperation.WriteUInt16Register, result.Operation);
    }

    private sealed class FakeClickPlcHandler : IClickPLCHandler
    {
        private readonly Dictionary<string, float> _floatRegisters = [];
        private readonly Dictionary<string, ushort> _uint16Registers = [];

        public ConcurrentQueue<int> ThreadIds { get; } = new();

        public bool FailNextWrite { get; init; }

        public bool IsOpen { get; private set; }

        public Grumpy.ClickPLCDriver.ILogRecord? LastRecord { get; private set; }

        public bool Open()
        {
            TrackThread();
            IsOpen = true;
            return true;
        }

        public bool Close()
        {
            TrackThread();
            IsOpen = false;
            return true;
        }

        public bool Init(string configJsonString)
        {
            TrackThread();
            return !string.IsNullOrWhiteSpace(configJsonString);
        }

        public bool ReadDiscreteControl(string name, out SwitchState state)
        {
            TrackThread();
            state = new SwitchState(SwitchSt.Off);
            return true;
        }

        public bool WriteDiscreteControl(string name, SwitchCtrl sw)
        {
            TrackThread();
            return true;
        }

        public bool ReadDiscreteControls(
            string name,
            int numberOfIosToRead,
            out SwitchState[] status)
        {
            TrackThread();
            status = Enumerable.Repeat(
                    new SwitchState(SwitchSt.Off),
                    numberOfIosToRead)
                .ToArray();
            return true;
        }

        public bool WriteDiscreteControls(string startName, SwitchCtrl[] controls)
        {
            TrackThread();
            return true;
        }

        public bool ReadInt16Register(string name, out short value)
        {
            TrackThread();
            value = 0;
            return true;
        }

        public bool WriteInt16Register(string name, short value)
        {
            TrackThread();
            return true;
        }

        public bool ReadUInt16Register(string name, out ushort value)
        {
            TrackThread();
            return _uint16Registers.TryGetValue(name, out value);
        }

        public bool WriteUInt16Register(string name, ushort value)
        {
            TrackThread();
            if (FailNextWrite) {
                return false;
            }

            _uint16Registers[name] = value;
            return true;
        }

        public bool ReadFloat32Register(string name, out float value)
        {
            TrackThread();
            return _floatRegisters.TryGetValue(name, out value);
        }

        public bool WriteFloat32Register(string name, float value)
        {
            TrackThread();
            _floatRegisters[name] = value;
            return true;
        }

        private void TrackThread() =>
            ThreadIds.Enqueue(Environment.CurrentManagedThreadId);
    }
}
