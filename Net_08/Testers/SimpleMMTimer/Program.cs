using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Grumpy.SDAQFramework.Drivers.MMTimer;
using Grumpy.SDAQFramework.MathUtilities;

namespace SimpleMMTimer
{
    internal class Program
    {
        //static uint userContext = 0; // User context variable for the timer
        private static int _uctr = 0; // Counter to track the number of ticks
        private static StatAccumulator? _accumulator;
        private static bool _stopRequested = false;
        private static int _dotPeriodMs = 2500;
        private const double _msToS = 0.001;
        private const int _sToMs = 1000; 
        private const double _clicksToMs = 0.0001;
        private const int _maxDotsPerLine = 40;
        static void Main(string[] args)
        {
            _accumulator = new StatAccumulator(25, true, -1);
            // Timer period in milliseconds
            uint periodMs = 3;
            // Timer resolution in milliseconds
            uint resolutionMs = 1;
            // Example user context value
            uint userCtx = 123;
            // Timer event type
            MMTimerEventType mMTimerEventType = MMTimerEventType.Periodic; 

            int runTimeMs = 0;
            Console.Write("Enter run time in seconds (0 for manual stop): ");

            if (!int.TryParse(Console.ReadLine(), out int runTimeSec) ||
                runTimeSec < 0) {
             
                runTimeSec = 0;
            }

            runTimeMs = runTimeSec * _sToMs;

            Thread timerThread = new Thread(() => {
                MultimediaTimerWrapper timer = new MultimediaTimerWrapper(
                    periodMs,
                    resolutionMs,
                    mMTimerEventType,
                    ref userCtx,
                    true,
                    Timer_Tick
                );

                if (runTimeMs > 0) {
                    Thread.Sleep(runTimeMs);
                    _stopRequested = true;
                    timer.Stop();
                }
                else {
                    while (!_stopRequested)
                        Thread.Sleep(_dotPeriodMs);
                    timer.Stop();
                }
            });

            timerThread.IsBackground = true;
            timerThread.Start();

            Stopwatch sw = Stopwatch.StartNew();
            int dots = 0;
            long lastElapsed = 0;

            while (timerThread.IsAlive) {

                Thread.Sleep(_dotPeriodMs);
                Console.Write(".");
                dots++;

                if (dots >= _maxDotsPerLine) {
                    // Time since the test started. 
                    long elapsed = sw.ElapsedMilliseconds;
                    // Print statistics before starting a new line of dots
                    Console.WriteLine($"[Lap {elapsed * _msToS:F2}s]");

                    if (_accumulator != null) {
                        Console.WriteLine($"Current statistics: {_uctr}");
                        Console.WriteLine(_accumulator.Result);
                    }

                    dots = 0;
                    lastElapsed = elapsed;
                }
            }

            if (dots > 0) {

                long elapsed = sw.ElapsedMilliseconds;
                Console.WriteLine($"[Total time {elapsed * _msToS:F2}s]");
                Console.WriteLine((_accumulator != null)
                    ? $"{_accumulator.Result}" 
                    : "No statistics available.");
            }

            sw.Stop();
            Console.WriteLine($"Timer stopped.\nTotal ticks: {_uctr}");
        }

        private static void Timer_Tick(object sender, MMTimerEventArgs e)
        {
            _uctr++;
            _accumulator?.AddValue(e.Clicks * _clicksToMs); // Convert clicks to milliseconds
        }
    }
}