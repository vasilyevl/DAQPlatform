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

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Grumpy.SDAQFramework.Drivers.MMTimer;
using Grumpy.SDAQFramework.Utilities.Testing;
using SDAQFramework.Math;



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

        static List<double>? data = new List<double>();
        static uint highResolution = 1;

        static StatAccumulator statAccum = null!;

        static void Main(string[] args) {
            int timerIncterval = 3; // ms
            int testDuration = 10000; // ms
            statAccum = new StatAccumulator(samplesToSkip: skip);

            Console.WriteLine($"Timer Tests. " +
                $"Timer interval: {timerIncterval}ms. " +
                $"Test duration: {testDuration/1000.0}s.");

            Console.WriteLine("\n#############################################################");
            count = -1;
            statAccum.Start();
            TestThreadingTimer(timerInterval: timerIncterval, testDuration);
            Report();
            ReportGenerator.TimingReport(data!, false);


            Console.WriteLine("\n#############################################################");
            count = -1;
            statAccum.Start();
            TestMultimediaTimer(timerInterval: (uint) timerIncterval, testDuration);
            Report();
            ReportGenerator.TimingReport(data!, false);
            return;
        }



        private static void TestMultimediaTimer(uint timerInterval = 10, int timeMs = -1)
        {
            data = new List<double>();

            var timer = new HighResTimer(
                periodMs: timerInterval,
                resolutionMs: highResolution,
                userCallback: null,
                operatingMode: TimerMode.Periodic,
                autoStart: false );

            Console.WriteLine("Console MM Timer");
            Console.WriteLine($"Timer interval: {timerInterval}ms. " +
                $"Resolution: {highResolution}ms.");
            timer.TimerEvent += EventHandler!;
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
            data.RemoveRange(0, skip);

        }
        
        private static void TestThreadingTimer(int timerInterval = 100, int timeMs = -1) 
        {
            data = new List<double>();

            Console.WriteLine("Console Startig Threading Timer");
            Console.WriteLine($"Timer interval: {timerInterval}ms.");
            Stopwatch s = new Stopwatch();
            
            using (var timer = new System.Timers.Timer()) {

                timer.Interval = timerInterval;
                
                timer.Elapsed += ((o, e) => {

                    double time = s.ElapsedTicks/10000.0;
                    data.Add(time);
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

                data.RemoveRange(0, skip);
            }
        }

        private static void EventHandler(object sender,
            TimerEventArgs e) {

            DoStats(e.Time);
            data.Add(e.Time);
        }


        private static void DoStats(double time)
        {
            if (count < 1) {
                previousTime = -1;
                skipCount= 0;
                count = 0;
                accum = 0;
                accum2 = 0;
                min = double.MaxValue;
                max = double.MinValue;
            }
            else {
                skipCount++;
                if (skipCount < skip) {

                    previousTime = time;
                }
                else { 
                    var delta = time - previousTime;
                    previousTime = time;
                    accum += delta;
                    accum2 += Math.Pow(delta, 2);
                    min = Math.Min(min, delta);
                    max = Math.Max(max, delta);
                    count++;
                }                   
            }
        }

        private static void Report()
        {
            var average = accum / count;

            var stdDev = Math.Sqrt((accum2 - Math.Pow(average, 2)*count)/(count-1));

            Console.WriteLine($"Timer test report:\n" +
                $"Count: {count}\n" +
                $"Average: {average.ToString(format)}ms\n" +
                $"StdDev: {stdDev.ToString(format)}ms\n" +
                $"Min: {min.ToString(format)}ms\n" +
                $"Max: {max.ToString(format)}ms\n" +
                $"Variance: {(stdDev * stdDev).ToString(format)}ms^2");

        }
    } 
}
