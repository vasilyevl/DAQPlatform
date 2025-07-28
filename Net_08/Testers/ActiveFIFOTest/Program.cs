using Grumpy.Common.BaseObjects.Collections;

namespace TestFIFOBase
{
    class Program
    {
        const int bufferSize = 64;
        const int producerCount = 4;
        const int consumerCount = 3;
        const int itemsPerProducer = 16;

        static void Main(string[] args) {

            // Create an instance of FIFOWReceiverBase with a specified capacity
            var fifo = new FIFOWReceiverBase<string>(bufferSize);

            // Subscribe to the ItemAdded event
            fifo.ItemAdded += OnItemAdded; 

            // Create and start producer tasks
            Task[] producers = new Task[producerCount];
            for (int i = 0; i < producerCount; i++) {
                int producerId = i + 1;
                producers[i] = Task.Run(() => ProduceItems(fifo, producerId));
            }

            // Create and start consumer tasks
            Task[] consumers = new Task[consumerCount];
            for (int i = 0; i < consumerCount; i++) {
                consumers[i] = Task.Run(() => ConsumeItems(fifo, i + 1));
                Thread.Sleep(25); // Simulate work
            }

            // Wait for all producers to finish
            Task.WaitAll(producers);

            // Signal the consumers to stop
            for (int i = 0; i < consumerCount; i++) {
                fifo.Push(null, out _); // Using null as a signal to stop
            }

            // Wait for all consumers to finish
            Task.WaitAll(consumers);

            Console.WriteLine("\n\nClick \"Enter\" to continue.");
            Console.ReadLine();

            var fifo1 = new FIFOWReceiverBase<string>(bufferSize);
            fifo1.ItemAdded += OnItemAdded;
            fifo1.HasReachedCapacity += (sender, e) => Console.WriteLine("Buffer #1 is at capacity.");
            fifo1.SetReceiver(ProcessItem);


            var producer5 = Task.Run(() => ProduceItems(fifo1, 5));
            Thread.Sleep(250); // Simulate work
            var produser6 = Task.Run(() => ConsumeItems(fifo1, 6));
            Console.WriteLine("\n\nClick \"Enter\" to exit.");
            Console.ReadLine();
        }

        private static void ProduceItems(FIFOBase<string> fifo, int producerId) {
            string error;
            for (int i = 0; i < itemsPerProducer; i++) {
                Console.WriteLine($"Producer {producerId} adding item {i + 1}.");
                string item = $"Producer {producerId} - Item {i + 1} added.";
                fifo.Push(item, out error);
                Thread.Sleep(50); // Simulate work
            }
        }

        private static void ConsumeItems(FIFOBase<string> fifo, int consumerId) {
            string error;
            Console.WriteLine($"Consumer {consumerId} starting.");
            while (true) {
                if (!fifo.IsEmpty) {
                    if (fifo.Pop(out var item, out error)) {
                        if (item == null) {
                            Console.WriteLine($"Consumer {consumerId} stopping.");
                            break; // Stop signal
                        }

                        Console.WriteLine($"Consumer {consumerId} popped item: {item}");
                    }
                    else {
                        Console.WriteLine($"Error popping item: {error}");
                    }
                    Thread.Sleep(50); // Simulate work
                }
            }
        }

        // Event handler for ItemAdded event
        private static void OnItemAdded(object? sender, int itemCount) {
            Console.WriteLine($"Item added. Current item count: {itemCount}");
        }

        // Receiver method to process items
        private static void  ProcessItem(string item) {

            Console.WriteLine($"Receiver. Processing item: {item}");
            Thread.Sleep(50);
      
        }
    }
}