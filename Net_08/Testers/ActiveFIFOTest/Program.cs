using Grumpy.SDAQFramework.Common;

namespace TestFIFOBase
{
    class Program
    {
        const int bufferSize = 256;
        const int producerCount = 8;
        const int consumerCount = 4;
        const int itemsPerProducer = 32;
        static FIFOWReceiverBase<string> fifo = new FIFOWReceiverBase<string>(bufferSize);
        
        static void Main(string[] args) {

            fifo.ItemAdded += OnItemAdded;
            fifo.HasReachedCapacity += (sender, e) => Console.WriteLine("Buffer #1 is at capacity.");
            fifo.SetReceiver(ProcessItem);

            var producer = Task.Run(() => ProduceItems(fifo, 1));
            producer.Wait();

            while (!fifo.IsEmpty) {
                Thread.Sleep(1);
            }

            Console.WriteLine("\n\nClick \"Enter\" to exit.");
            Console.ReadLine();
        }

        private static void ProduceItems(FIFOBase<string> fifo, int producerId, int sleepMs = -1) {
            string error;

            for (int i = 0; i < itemsPerProducer; i++) {

                string item = $"Producer {producerId} - Item {i + 1}.";
                fifo.Push(item, out error);
                Console.WriteLine($"Producer {producerId} item {i + 1} added. " +
                    $"Items in the buffer: {fifo.Count}");
                if (sleepMs > 0) {
                    Thread.Sleep(sleepMs);
                }// Simulate work
            }
        }

        // Event handler for ItemAdded event
        private static void OnItemAdded(object? sender, int itemCount) {
            Console.WriteLine($"Event. Item added. Current item count: {itemCount}");
        }

        // Receiver method to process items
        private static void  ProcessItem(string item) {

            Console.WriteLine($"Receiver. Processing item: {item}. " +
                $"Number of items in the buffer: {fifo.Count}");
        // Thread.Sleep(50);

        }
    }
}