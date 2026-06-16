using System.Collections.Concurrent;
using Grumpy.SDAQFramework.Common;

namespace StatePatternFrameworkTests;

public sealed class DedicatedReceiverTests
{
    [Fact]
    public void Receiver_ProcessesAllItemsOnOnePersistentThread()
    {
        var received = new ConcurrentQueue<int>();
        var threadIds = new ConcurrentQueue<int>();
        using var processed = new CountdownEvent(3);
        using var receiver = new DedicatedReceiver<int>(
            "TestReceiver",
            capacity: 8,
            (item, _) => {
                received.Enqueue(item);
                threadIds.Enqueue(Environment.CurrentManagedThreadId);
                processed.Signal();
            });

        receiver.Start();
        Assert.True(receiver.TrySubmit(1, out string firstError), firstError);
        Assert.True(receiver.TrySubmit(2, out string secondError), secondError);

        Assert.True(SpinWait.SpinUntil(
            () => received.Count == 2,
            TimeSpan.FromSeconds(2)));

        Thread.Sleep(25);
        Assert.True(receiver.TrySubmit(3, out string thirdError), thirdError);
        Assert.True(processed.Wait(TimeSpan.FromSeconds(2)));

        Assert.Equal([1, 2, 3], received.ToArray());
        Assert.Single(threadIds.Distinct());
        Assert.Equal(receiver.ThreadId, threadIds.First());
    }

    [Fact]
    public void Receiver_StopsWhileIdle()
    {
        using var receiver = new DedicatedReceiver<int>(
            "IdleReceiver",
            capacity: 4,
            (_, _) => { });

        receiver.Start();

        Assert.True(receiver.Stop(TimeSpan.FromSeconds(2)));
        Assert.False(receiver.IsRunning);
    }

    [Fact]
    public void Receiver_ReportsCallbackFailuresAndContinues()
    {
        using var faultObserved = new ManualResetEventSlim();
        using var secondItemProcessed = new ManualResetEventSlim();
        using var receiver = new DedicatedReceiver<int>(
            "FaultReceiver",
            capacity: 4,
            (item, _) => {
                if (item == 1) {
                    throw new InvalidOperationException("Expected failure.");
                }

                secondItemProcessed.Set();
            });

        receiver.Faulted += (_, args) => {
            Assert.Equal(1, args.Item);
            faultObserved.Set();
        };

        receiver.Start();
        Assert.True(receiver.TrySubmit(1, out string firstError), firstError);
        Assert.True(receiver.TrySubmit(2, out string secondError), secondError);

        Assert.True(faultObserved.Wait(TimeSpan.FromSeconds(2)));
        Assert.True(secondItemProcessed.Wait(TimeSpan.FromSeconds(2)));
    }
}
