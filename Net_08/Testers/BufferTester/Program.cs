using Grumpy.SDAQFramework.Common;
using Grumpy.SDAQFramework.Utilities.Testing;

namespace BufferBaseConsoleTest
{
    class Program
    {
        const int bufferSize = 256;
        const int producerCount = 8;
        const int consumerCount = 4;
        const int itemsPerProducer = 32;

        static void Main() {
            Console.WriteLine("BufferBase<T> Console Test\n");

            TestBasicOperations();
            TestEvents();
            TestTryAddItemsAndCapacity();
            TestMakeRoom();
            TestMultithreaded();
            TestPeekAll();
            FifoTest();
        }

        static void TestBasicOperations() {

            int bufferDepth = 5;
            Console.WriteLine("TestBasicOperations...");
            var buffer = new BufferBase<int>(bufferDepth);
            Console.WriteLine($"\tBasic buffer with depth #{bufferDepth}: " +
                $"current count: {buffer.Count}.");
            string error;

            for (int i = 0; i < bufferDepth; i++) {
                TestUtilities.AssertOrNotify(buffer.TryAdd(i, out error), 
                    $"Add {i}", message: error);
            }

            Console.WriteLine($"\t\tCount after adds: {buffer.Count}");
            
            TestUtilities.AssertOrNotify(buffer.HasRoom, "HasRoom", 
                "Buffer has room.", "No room", accepableFalse: true);
            TestUtilities.AssertOrNotify(!buffer.IsEmpty, "IsEmpty", 
                "Buffer is not empty.");
            TestUtilities.AssertOrNotify(buffer.IsAtCapacity, 
                "IsAtCapacity","Buffer is at capacity.");

            int val;
            TestUtilities.AssertOrNotify(buffer.TryPopAt(0, out val, out error), 
                "PopAt(0)", message: error);
            Console.WriteLine($"\t\tPopped value is: {val}.");           

            TestUtilities.AssertOrNotify(buffer.TryPeekAt(0, out val, out error), 
                "PeekAt(0)", message: error);
            Console.WriteLine($"\t\tPeeked value {val}.");

            for (int i = 5; i < 7 - 1; i++) {
                TestUtilities.AssertOrNotify(buffer.TryAdd(i, out error), 
                    $"Add {i}", message: error);
            }

            int val1;
            int val2;

            if (TestUtilities.AssertOrNotify(buffer.TryPeekLast(out val1, out error), 
                "PeekLast", message: error)) {
                Console.WriteLine($"\t\tPeeked last value: {val1}.");
            }

            if (TestUtilities.AssertOrNotify(buffer.TryPopLast(out val2, out error), 
                "PopLast", message: error)) {
                Console.WriteLine($"\t\tPopped last value: {val2}.");
            }
 
            TestUtilities.AssertOrNotify(buffer.IsEmpty, "IsEmpty" , 
                "Buffer is empty", "Buffer is not empty", true);
            TestUtilities.AssertOrNotify(buffer.HasRoom, "Has room", 
                "Buffer Has room", "Buffer does not have room", true);

            Console.WriteLine("\t\tTestBasicOperations Passed.\n");
        }

        static void TestEvents() {

            Console.WriteLine("TestEvents...");
            int upperThreshould = 7;
            int loverThreshold = 3;
            int capacity = 9;
            var buffer = new BufferBase<int>(capacity);
            var events = new List<string>();

            buffer.HasBecomeEmpty += (s, e) => { lock (events) { 
                    events.Add("Empty"); } 
            };

            buffer.HasReachedCapacity += (s, e) => { lock (events) { 
                    events.Add("Capacity"); } 
            };

            buffer.AtUpperThreshold += (s, e) => { lock (events) { 
                    events.Add("Upper"); } 
            };

            buffer.AtLowerThreshold += (s, e) => { lock (events) { 
                    events.Add("Lower"); } 
            };
            buffer.ItemAdded += (s, count) => { 
                lock (events) { events.Add($"Added:{count}"); } 
            };

            buffer.ItemsDiscarded += (s, count) => { 
                lock (events) { events.Add($"Discarded:{count}"); } 
            };

            buffer.UpperThreshold = upperThreshould;
            buffer.LowerThreshold = loverThreshold;

            buffer.TryAdd(1, out _); // Should trigger ItemAdded
            buffer.TryAdd(2, out _); // Should trigger ItemAdded, Upper
            buffer.TryAdd(3, out _); // Should trigger ItemAdded, Capacity
            buffer.TryAddItems(new[] { 4, 5, 6, 7, 8, 9 } , 
                out _, out string error); // Should trigger ItemAdded, Capacity

            bool popLast = false;
            while (buffer.Count > 0) {
                popLast = !popLast; // Alternate between popping last and first
                if (popLast) {
                    buffer.TryPopLast(out _, out _);
                }
                else {
                    buffer.TryPopFirst(out _, out _);
                }
            }

            // Wait for async events
            Thread.Sleep(300);

            lock (events) {
                TestUtilities.AssertOrNotify(events.Any(e => e.StartsWith("Added")), "Events", "Event: ItemAdded");
                TestUtilities.AssertOrNotify(events.Contains("Upper"), "Events", "Event: WentOverUpperThreshold");
                TestUtilities.AssertOrNotify(events.Contains("Capacity"), "Events", "Event: HasReachedCapacity");
                TestUtilities.AssertOrNotify(events.Contains("Lower"), "Events", "Event: DroppedBelowLowerThreshold", accepableFalse: true);
                TestUtilities.AssertOrNotify(events.Contains("Empty"), "Events", "Event: HasBecomeEmpty", accepableFalse: true);
            }

            TestUtilities.PrintInfo("Events fired: " + string.Join(", ", events), 4);
            TestUtilities.PrintComment("TestEvents Passed.\n");
        }

        static void TestTryAddItemsAndCapacity() {
            TestUtilities.PrintComment("TestTryAddItemsAndCapacity...");
            var buffer = new BufferBase<int>(4);
            string error;
            int itemsAdded = 0;
            TestUtilities.AssertOrNotify(buffer.TryAddItems(new[] { 1, 2 }, 
                out itemsAdded, out error), "TryAddItems", "Add Items [1,2]", error, true);
            TestUtilities.AssertOrNotify(buffer.TryAddItems(new[] { 3 }, 
                out itemsAdded, out error), "TryAddItems", "AddItems [3]", error, true);
            TestUtilities.AssertOrNotify(!buffer.TryAddItems(new[] { 4, 5 }, 
                out itemsAdded, out error), "TryAddItems", "AddItems [4,5] fails");
            TestUtilities.AssertOrNotify(error.Contains("Not enough room"), 
                "Error contains 'Not enough room'", true);
            TestUtilities.PrintComment("TestTryAddItemsAndCapacity Passed.\n");
        }

        static void TestMakeRoom() {
            int bufferCapacity = 10;
            TestUtilities.PrintComment("TestMakeRoom...");
            var buffer = new BufferBase<int>(bufferCapacity);
            string error;
            int itemsAdded = 0;
            TestUtilities.AssertOrNotify(buffer.TryAddItems(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 
                out itemsAdded, out error),
                $"Add [1, 2,... 10]", message: error);

            int removed;
            int remove = 4;
            TestUtilities.AssertOrNotify(buffer.TryMakeRoom(remove, out removed, out error), 
                $"TryMakeRoom({remove})", "", error, accepableFalse: true);
            TestUtilities.AssertOrNotify(removed == remove, "Checking removed count.", 
                $"Removed count == {remove}", $"Removed count= {removed}", accepableFalse: true);
            TestUtilities.AssertOrNotify(buffer.Count == (bufferCapacity - remove), 
                "Checking buffer count.", "Count == 2", $"Count = {buffer.Count}", accepableFalse: true);

            int remove2 = 11;
            TestUtilities.AssertOrNotify(!buffer.TryMakeRoom(remove2, out removed, out error), 
                $"TryMakeRoom({remove2})", "", error, accepableFalse: true);
            TestUtilities.AssertOrNotify(removed != remove, "Checking removed count.", 
                $"Removed count == {remove2}", $"Removed count= {removed}", accepableFalse: true);
            TestUtilities.AssertOrNotify(buffer.Count != (bufferCapacity - remove - remove2), 
                "Checking buffer count.", $"Count == {bufferCapacity - remove - remove2}", 
                $"Count = {buffer.Count}", accepableFalse: true);


            TestUtilities.PrintComment("TestMakeRoom Passed.\n");


        }

        static void TestMultithreaded() {
            TestUtilities.PrintComment("TestMultithreaded...");
            var buffer = new BufferBase<int>(100);
            int addSuccess = 0, popSuccess = 0;
            int threadCount = 8;
            int itemsPerThread = 1000;

            var addTasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() => {
                for (int i = 0; i < itemsPerThread; i++) {
                    string err;
                    if (buffer.TryAdd(i, out err))
                        Interlocked.Increment(ref addSuccess);
                }
            })).ToArray();

            var popTasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() => {
                for (int i = 0; i < itemsPerThread; i++) {
                    int v;
                    string err;
                    if (buffer.TryPopLast(out v, out err))
                        Interlocked.Increment(ref popSuccess);
                }
            })).ToArray();

            Task.WaitAll(addTasks.Concat(popTasks).ToArray());

            TestUtilities.AssertOrNotify(buffer.Count >= 0 && buffer.Count <= buffer.MaxCapacity, "", "Final Count in [0, MaxCapacity]");
            TestUtilities.AssertOrNotify(addSuccess > 0, "Checking number of successfull adds", $"addSuccess = {addSuccess}");
            TestUtilities.AssertOrNotify(popSuccess >= 0, "Checking number of successfull pops", $"popSuccess {popSuccess}");

            TestUtilities.PrintComment($"AddSuccess: {addSuccess}, PopSuccess: {popSuccess}, FinalCount: {buffer.Count}", 8);
            TestUtilities.PrintComment("TestMultithreaded Passed.\n", 8);
        }

        static void TestPeekAll() {
            TestUtilities.PrintComment("TestPeekAll...");
            var buffer = new BufferBase<int>(10);
            int itemsAdded = 0;
            string error = string.Empty;

            buffer.TryAddItems(new[] { 1, 2, 3 }, out itemsAdded, out error);

            var arr = buffer.PeekAllAsArray(out error);
            TestUtilities.AssertOrNotify(arr.SequenceEqual(new[] { 1, 2, 3 }), "PeekAllAsArray", "Peek matches.", "Peek does not match");

            var arrRev = buffer.PeekAllAsArray(out error, recentFirst: true);
            TestUtilities.AssertOrNotify(arrRev.SequenceEqual(new[] { 3, 2, 1 }), "PeekAllAsArray (recentFirst)", "Peek matches.", "Peek does not match");

            var list = buffer.PeekAllAsList(out error);
            TestUtilities.AssertOrNotify(list.SequenceEqual(new List<int> { 1, 2, 3 }), "PeekAllAsList", "Peek matches.", "Peek does not match");

            var listRev = buffer.PeekAllAsList(out error, recentFirst: true);
            TestUtilities.AssertOrNotify(listRev.SequenceEqual(new List<int> { 3, 2, 1 }), "PeekAllAsList (recentFirst)", "Peek matches.", "Peek does not match");

            TestUtilities.PrintComment("TestPeekAll Passed.\n");
        }

        private static void FifoTest()
        {
            // Create an instance of FIFOWReceiverBase with a specified capacity
            var fifo = new FIFOBase<string>(bufferSize);

            // Subscribe to the ItemAdded event
            fifo.ItemAdded += OnItemAdded;
            fifo.HasReachedCapacity += (sender, e) =>
                Console.WriteLine("Buffer is at capacity.");


            // Create and start producer tasks
            Task[] producers = new Task[producerCount];
            for (int i = 0; i < producerCount; i++) {

                int producerId = i + 1;
                producers[i] = Task.Run(() => ProduceItems(fifo, producerId));
            }

            // Create and start consumer tasks
            Task[] consumers = new Task[consumerCount];
            int[] indexes = new int[consumerCount];
            for (int i = 0; i < consumerCount; i++) {

                indexes[i] = i + 1;
                consumers[i] = Task.Run(() =>
                    ConsumeItems(fifo, indexes[i], indexes[i] * 25));
                Thread.Sleep(100); // Simulate work
            }

            // Wait for all producers to finish
            Task.WaitAll(producers);

            // Signal the consumers to stop
            for (int i = 0; i < consumerCount; i++) {

                fifo.Push(null, out _); // Using null as a signal to stop
            }

            // Wait for all consumers to finish
            Task.WaitAll(consumers);

            Console.WriteLine("\n\nClick \"Enter\" to exit.");
            Console.ReadLine();
        }

        private static void ProduceItems(FIFOBase<string> fifo, int producerId)
        {
            string error;

            for (int i = 0; i < itemsPerProducer; i++) {

                Console.WriteLine($"Producer {producerId} adding item {i + 1}.");
                string item = $"Producer {producerId} - Item {i + 1}.";
                fifo.Push(item, out error);
                Console.WriteLine($"Producer {producerId} item {i + 1} added.");
                Thread.Sleep(33); // Simulate work
            }
        }

        private static void ConsumeItems(FIFOBase<string> fifo,
            int consumerId,
            int periodMs = 50)
        {

            string error;

            Console.WriteLine($"Consumer {consumerId} starting.");

            while (true) {

                if (!fifo.IsEmpty) {

                    if (fifo.Pop(out var item, out error)) {

                        if (item == null) {
                            Console.WriteLine($"Consumer {consumerId} stopping.");
                            // Stop signal
                            break;
                        }

                        Console.WriteLine($"Consumer {consumerId} popped item: [{item}]");
                    }
                    else {
                        Console.WriteLine($"Error popping item: {error}");
                    }
                    // Simulate work
                    Thread.Sleep(periodMs);
                }
            }
        }

        // Event handler for ItemAdded event
        private static void OnItemAdded(object? sender, int itemCount)
        {
            Console.WriteLine($"Item added. Current item count: {itemCount}");
        }

        // Receiver method to process items
        private static void ProcessItem(string item)
        {

            Console.WriteLine($"Receiver. Processing item: {item}");
            Thread.Sleep(50);

        }
    }
}