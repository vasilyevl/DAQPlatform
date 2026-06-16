using Grumpy.SDAQFramework.Common;

namespace TestFIFOBase;

internal static class Program
{
    private const int BufferSize = 256;
    private const int ItemsPerProducer = 32;

    private static readonly CountdownEvent ItemsProcessed =
        new(ItemsPerProducer);

    private static readonly DedicatedReceiver<string> Receiver = new(
        "ActiveFIFOTest",
        BufferSize,
        ProcessItem);

    private static void Main()
    {
        Receiver.Start();

        Task producer = Task.Run(() => ProduceItems(Receiver, producerId: 1));
        producer.Wait();
        ItemsProcessed.Wait();

        Receiver.Dispose();

        Console.WriteLine("\n\nClick Enter to exit.");
        Console.ReadLine();
    }

    private static void ProduceItems(
        DedicatedReceiver<string> receiver,
        int producerId,
        int sleepMs = -1)
    {
        for (int index = 0; index < ItemsPerProducer; index++) {
            string item = $"Producer {producerId} - Item {index + 1}.";

            if (!receiver.TrySubmit(item, out string error)) {
                throw new InvalidOperationException(error);
            }

            Console.WriteLine(
                $"Producer {producerId} item {index + 1} added. " +
                $"Items in the buffer: {receiver.PendingCount}");

            if (sleepMs > 0) {
                Thread.Sleep(sleepMs);
            }
        }
    }

    private static void ProcessItem(
        string item,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(
            $"Receiver processing: {item}. " +
            $"Items in the buffer: {Receiver.PendingCount}");
        ItemsProcessed.Signal();
    }
}
