using System;
using System.Threading;
using Grumpy.Common.BaseObjects;

namespace DormantWorkerConsoleTest
{
    class Program
    {

        static int counter = 0; 
        static int iterations = 0;
        static void Main() {
            Console.WriteLine("DormantWorker Console Test\n");

            TestSingleResume();
            TestMultipleResume();
            TestPauseResume();
            TestDisposeStopsWorker();

            Console.WriteLine("\nAll tests completed.");
        }

        static void TestSingleResume() {

            Console.WriteLine("TestSingleResume...");
            counter = 0;
            iterations = 1; // Set to 1 for single run test


            var worker = new DormantWorker(WorkAction);
           
                Thread.Sleep(100);
                AssertOrNotify(counter == 0, $"Should not run before Resume(). Ran {counter} times.");

                counter= 0; // Reset counter for this test
                worker.Resume();
                Thread.Sleep(100);
                AssertOrNotify(counter == 1, "Should run once after Resume()");

                Thread.Sleep(200);
                AssertOrNotify(counter == 1, "Should not run again without Resume()");

                worker.Resume();
                Thread.Sleep(100);
                AssertOrNotify(counter == 2, $"Should run once with 1 iteration after resume,  Ran {counter} times total.");
            
            Console.WriteLine("Disposing worker.");
            worker.Dispose(); // Ensure worker is disposed after test
            
            Console.WriteLine("Single Resume Test complete.");
        }

        static void TestMultipleResume() {
            Console.WriteLine("TestMultipleResume...");
            int counter = 0;
            iterations = 1; // Set to 5 for multiple runs test
            using (var worker = new DormantWorker(WorkAction)) {
                for (int i = 0; i < 5; i++) {
                    worker.Resume();
                    Thread.Sleep(100);
                }
                Thread.Sleep(100);
                AssertOrNotify(counter == 5, $"Should run 5 times after 5 Resume() calls. Ran {counter} times.");
            }
            Console.WriteLine();
        }

        static void TestPauseResume() {
            Console.WriteLine("TestPauseResume...");
            counter = 0;
            iterations = 1; // Set to 1 for single run test

            using (var worker = new DormantWorker(WorkAction)) {
                worker.Resume();
                Thread.Sleep(100);
                AssertOrNotify(counter == 1, "Should run once after Resume()");

                worker.Pause();
                Thread.Sleep(100);
                AssertOrNotify(counter == 1, "Should not run again after Pause() without Resume()");

                worker.Resume();
                Thread.Sleep(100);
                AssertOrNotify(counter == 2, "Should run again after Resume()");
            }
            Console.WriteLine();
        }

        static void TestDisposeStopsWorker() {
            Console.WriteLine("TestDisposeStopsWorker...");
            int counter = 0;
            var worker = new DormantWorker(WorkAction);
            worker.Resume();
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

            Console.WriteLine("Worker started.");

            for (int i = 0; i < iterations; i++) {

                Console.WriteLine($"Working... {i+1}");
                Interlocked.Increment(ref counter);
                //Thread.Sleep(50); // Simulate work
            }

        }
    }
}