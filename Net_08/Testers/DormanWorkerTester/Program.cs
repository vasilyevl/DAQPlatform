using Grumpy.SDAQFramework.Common;
using System;
using System.Text;
using System.Threading;

namespace DormantWorkerConsoleTest
{
    class Program
    {
        static int counter = 0; 
        static int iterations = 0;
        static bool once = false;

        static void Main() {
            Console.WriteLine("DormantWorker Console Test\n");

            //TestSingleResume();
            //TestSingleResume( 5 );
            //TestSingleResume( 10 );
            TestMultipleResume(itrtns: 2, runs: 5, sleepTime: 100);
            TestMultipleResume(itrtns: 1, runs: 15, sleepTime: 33);
            //TestMultipleResume(itrtns: 5, runs: 25, sleepTime: 33);
            //TestPauseResume();
            //TestDisposeStopsWorker();

            Console.WriteLine("\nAll tests completed.");
        }

        static void TestSingleResume(int itrtns = 1) {

            Console.WriteLine("TestSingleResume...");
            counter = 0;
            iterations = itrtns; // Set to 1 for single run test

            var worker = new DormantWorker(WorkAction);
           
            Thread.Sleep(100);
            AssertOrNotify(counter == 0, $"Should not run before WakeUp(). Ran {counter} times.");

            counter= 0; // Reset counter for this test
            Console.WriteLine($"Waiking up worker. Counter value: {counter}");
            worker.Trigger();
            Thread.Sleep(200);
            Console.WriteLine($"Delay executed. Counter value: {counter}");

            AssertOrNotify(counter == itrtns, $"Should run #{itrtns} time(s) after WakeUp()");

            Thread.Sleep(200);
            AssertOrNotify(counter == itrtns, "Should not run again without WakeUp()");
            once = true;

            Console.WriteLine($"Waking up worker. Counter value: {counter}");
            worker.Trigger();
            Thread.Sleep(200);
            Console.WriteLine($"Delay executed. Counter value: {counter}");
            AssertOrNotify(counter == 2*itrtns, $"Should run #{itrtns} time(s) after resume,  Ran {counter} times total.");

            Console.WriteLine($"Waking up worker. Counter value: {counter}");
            Console.WriteLine("Disposing worker.");
            worker.Dispose(); // Ensure worker is disposed after test
            
            Console.WriteLine("Single Resume Test complete.");
        }

        static void TestMultipleResume(int itrtns =  1, int runs = 5, int sleepTime = 100) {

            Console.WriteLine("TestMultipleResume...");
            
            iterations = itrtns; // Set to runs for multiple runs test

                var worker = new DormantWorker(WorkAction);                    
                int startCounter = counter;

                for (int i = 0; i < runs; i++) {
                    Console.WriteLine($"Run #{i+1}.");
                    worker.Trigger();
                    Thread.Sleep(sleepTime);
                }

                Thread.Sleep(sleepTime*5);
                AssertOrNotify((counter - startCounter) == runs,
                    $"Expected to run #{runs*itrtns} times after #{runs} Resume() calls. " +
                    $"Ran {counter - startCounter} times.");
                
                worker.Dispose();
            
            Console.WriteLine();
        }

        static void TestPauseResume() {
            Console.WriteLine("TestPauseResume...");
            counter = 0;
            iterations = 1; // Set to 1 for single run test

            using (var worker = new DormantWorker(WorkAction)) {
                worker.Trigger();
                Thread.Sleep(100);
                AssertOrNotify(counter == 1, "Should run once after Resume()");

                worker.IsPaused = true;
                Thread.Sleep(100);
                AssertOrNotify(counter == 1, "Should not run again after Pause() without Resume()");

                worker.Trigger();
                Thread.Sleep(100);
                AssertOrNotify(counter == 2, "Should run again after Resume()");
            }
            Console.WriteLine();
        }

        static void TestDisposeStopsWorker() {
            Console.WriteLine("TestDisposeStopsWorker...");
            int counter = 0;
            var worker = new DormantWorker(WorkAction);
            worker.Trigger();
            Thread.Sleep(100);
            worker.Dispose();

            int countAfterDispose = counter;
            Thread.Sleep(200);
            AssertOrNotify(counter == countAfterDispose, "Should not run after Dispose()");
            Console.WriteLine();
        }

        static void AssertOrNotify(bool condition, string message) {
            
            if (condition) {
            
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [PASS] {message}");
            }
            else {
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [FAIL] {message}");
            }
            Console.ResetColor();
        }

        static void WorkAction() {

            Console.WriteLine($"Worker started. #{iterations} requested.");

            for (int i = 0; i < iterations; i++) {

                Console.WriteLine($"Working... {i+1}, counter value is {++counter}");
                ;
                // Interlocked.Increment(ref counter);
                //Thread.Sleep(50); // Simulate work
            }

        }
    }
}