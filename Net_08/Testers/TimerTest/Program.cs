using System.Diagnostics;
using Grumpy.DAQFramework.Drivers.MMTimer;
using System.Runtime.InteropServices;
using System.Timers;

namespace TimerTest
{
    internal class Program
    {
        //static TimerCaps _caps = new TimerCaps();

        static uint _period = 1;
        static uint _resolution = 1;

        static uint _testDuration = 10000;

        static double _start = 0;
        static List<double> _times = new List<double>();
        static List<double> _durations = new List<double>();
        static double _stopWatchTicksPerMS;
        static Stopwatch? _stopWatch;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            _times.Clear();
            _stopWatchTicksPerMS = Stopwatch.Frequency / 1000.0;
            _stopWatch = Stopwatch.StartNew();

            var timer = new HighResTimer(periodMs: _period,
                resolutionMs: _resolution,
                userCallback: TimerProc,
                operatingMode: TimerMode.Periodic,
                autoStart: false);

            _start = _stopWatch.ElapsedTicks / _stopWatchTicksPerMS;

            timer.Start();
            _start = timer.StartTimeMs;
            Thread.Sleep((int)_testDuration);

            timer.Stop();

            Console.WriteLine($"MM timer \"Test3\" complete.\n" +
                $"\tTotal processed tickes: {timer.TickCounter}.\n" +
                $"\tTotal missed ticks: {timer.MisedTickCounter}.");

            ReportResults();
        }


        static private void TimerProc(int timerID, ulong clicks, double time)
        {
            _times.Add(time);
        }



        static private void ReportResults(bool showHeader = true)
        {
            if (showHeader)
            {
                Console.WriteLine($"Start Time: {_start.ToString()}ms.");
                Console.WriteLine($"Period: {_period}ms");

                Console.WriteLine($" ## \tTime \tDelta.\tError ms.\tError %");
            }
            for (int i = 0; i < _times.Count; i++)
            {
                Double delta = (_times[i] - ((i == 0) ? _start : _times[i - 1]));
                var error = delta - _period;
                Console.WriteLine($" {i + 1}." +
                    $"{(i < 10 ? " " : "")}" +
                    $"\t{(_times[i] - _start)}" +
                    $"\t{(delta >= 0 ? " " : "")}" +
                    $"{delta.ToString("F3")}" +
                    $"\t   {(delta >= 0 ? " " : "")}" +
                    $"{error.ToString("F3")}" +
                    $"\t{(delta >= 0 ? " " : "")}" +
                    $"{(100.0 * error / _period).ToString("F2")}");
            }
        }
    }
}
