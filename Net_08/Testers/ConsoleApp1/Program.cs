/*
Copyright (c) 2024, 2025 vasilyevl (Grumpy). Permission is hereby granted,
free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"),to deal in the Software
without restriction, including without limitation the rights to use, copy,
modify, merge, publish, distribute, sublicense, and/or sell copies of the
Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,FITNESS FOR A
PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Grumpy.SDAQFramework.Drivers.MMTimer;
using Grumpy.SDAQFramework.Utilities.Testing;
using SDAQFramework.MathUtilities;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;



namespace MMTimerTest
{
    internal class Program {


       
        internal static double previousTime = 0;
        internal static int skip = 10;
        internal static int skipCount = 0;  
        internal static double accum = 0;
        internal static double accum2 = 0;
        internal static int count = 0;
        internal static double min = double.MaxValue;
        internal static double max = double.MinValue;
        internal static string format = "F2";

        static uint highResolution = 1;

        static StatAccumulator statAccum = null!;
        // Declare a field to hold a strong reference to the delegate
        private static UserTimerProc? _myTyperProcInstance;


        static void Main(string[] args) {

            int timerIncterval = 3; // ms
            int testDuration = 1000*60*10; // ms
            statAccum = new StatAccumulator(samplesToSkip: skip, autoStart: false);

            Console.WriteLine($"Timer Tests. " +
                $"Timer interval: {timerIncterval}ms. " +
                $"Test duration: {testDuration/1000.0}s.");

            try {
                /*
                            Console.WriteLine("\n#############################################################");
                            statAccum.Start();
                            TestThreadingTimer(timerInterval: timerIncterval, testDuration);
                            statAccum.Stop();
                            Console.WriteLine("Threading Timer test completed.");
                            Console.WriteLine("Report:");
                            Console.WriteLine(statAccum.Result);
                */
                Console.WriteLine("\n#############################################################");

                statAccum.Reset(start: true);
                TestMultimediaTimer(timerInterval: (uint) timerIncterval, testDuration);
                statAccum.Stop();
                Console.WriteLine("Multimedia Timer test completed.");
                Console.WriteLine("Report:");
                Console.WriteLine(statAccum.Result);

                return;
            }
            catch(Exception e) {
                Console.WriteLine($"/n/n{e}");
            }
        }

        private static void TestMultimediaTimer(uint timerInterval = 10, int timeMs = -1)
        {
           // _myEventHandlerInstance = EventHandler;
            count = 0;

            _myTyperProcInstance = MyTymerProc;
            var timer = new HighResTimer(
                periodMs: timerInterval,
                resolutionMs: highResolution,
                userCallback: null,
                operatingMode: TimerMode.Periodic,
                autoStart: false );
            GCHandle handle = GCHandle.Alloc(timer);
            Console.WriteLine("Console MM Timer");
            Console.WriteLine($"Timer interval: {timerInterval}ms. " +
                $"Resolution: {highResolution}ms.");
            //timer.TimerEvent += _myEventHandler;
            timer.Start();

            if (timeMs < 1) {

                Console.WriteLine("Click any key to stop.");
                Console.ReadKey();
            }
            else {
                
                Console.WriteLine($"Running timer test for {timeMs/1000.0}s.");
                Thread.Sleep(timeMs+(int)timerInterval);
            }
            timer.Stop();
            Thread.Sleep(100); // Give time to process last event
            handle.Free();
            timer.Dispose();
            Console.WriteLine();
        }
        
        private static void TestThreadingTimer(int timerInterval = 100, int timeMs = -1) 
        {
            count = 0;

            Console.WriteLine("Console Startig Threading Timer");
            Console.WriteLine($"Timer interval: {timerInterval}ms.");
            Stopwatch s = new Stopwatch();
            
            using (var timer = new System.Timers.Timer()) {

                timer.Interval = timerInterval;
                
                timer.Elapsed += ((o, e) => {

                    double time = s.ElapsedTicks/10000.0;
                    DoStats(time);
                    return;
                });
                
                timer.AutoReset = true;       
                timer.Enabled = true;

                s.Start();
                if (timeMs < 1) {
                    Console.WriteLine("Click any key to stop.");
                    Console.ReadKey();
                }
                else {
                    Console.WriteLine($"Running timer test for {timeMs/1000.0}s.");
                    Thread.Sleep(timeMs + timerInterval);
                }
            }
        }


        static ulong eventCount = 0;
        private static void EventHandler(object sender,
            TimerEventArgs e) {
            Console.Write($"Event #{++eventCount}\r");
            DoStats(e.Time);
        }


        private static void MyTymerProc(int timerID, ulong tickNumber, double timeMs)
        {
            Console.Write($"Event #{++eventCount}    {timeMs.ToString("F2")}ms\r ");

            //DoStats(timeMs/1000.0);
        }

        private static void DoStats(double time)
        {
            if (count < 1) {
                previousTime = time;
            }
            else {
 
                    statAccum.AddValue(time - previousTime);
                    previousTime = time;                   
            }
            count++;
        }
    } 
}
