using Grumpy.Utilities.MMTimer;
using System.Runtime.InteropServices;


namespace MMTimertTester
{
    internal class Program
    {
        static TimerCaps _caps = new TimerCaps();
        static uint _delay = 1;
        static uint _resolution = 1;
        static DateTime _start = DateTime.Now;
        static List<DateTime> _times = new List<DateTime>();
        static int _sleepMs = 1000;

        static void Main(string[] args) {

            Console.WriteLine("Test started");
       
            Console.WriteLine("Test1 (\"Periodic\") started.");

            Int32 r = NativeMMTimerWrap.GetDevCaps(ref _caps,
                     Marshal.SizeOf<TimerCaps>(Program._caps));

            if (r != 0) {
                Console.WriteLine("Failed to get MMTimer caps.");
                return;
            }

            Console.WriteLine("MM timer caps received: \n" +
                    $"Min - {_caps.PeriodMin}ms, max - {_caps.PeriodMax}ms.");

            _start = DateTime.Now;

            _times.Clear();

            UInt32 resolution = Math.Max(_resolution, _caps.PeriodMin);
            UInt32 err = NativeMMTimerWrap.BeginPeriod(resolution);
            
            Int32 id = NativeMMTimerWrap.SetEvent(_delay,
                    0, TimerCallback,
                    123,
                    (uint)TimerMode.Periodic);

            if (id == 0) {
                Console.WriteLine("Failed to set MM timer event.");
                return;
            }
            
            Thread.Sleep(_sleepMs);

            NativeMMTimerWrap.KillEvent(id);
            NativeMMTimerWrap.EndPeriod(resolution);
            
            ReportResults();
        }

        static private void ReportResults() {

            Console.WriteLine($"Start Time: {_start.ToString("HH:mm:ss.fff")}");
            Console.WriteLine($"Period: {_delay}ms. Test duration {_sleepMs}ms");
            Console.WriteLine($"Total number of events captured: {_times.Count} each.");

            for (int i = 0; i < _times.Count; i++) {
                var delta = (_times[i] - ((i == 0) ? _start : _times[i - 1])).TotalMilliseconds;
                var error = delta - _delay;
                Console.WriteLine($"Delta: {delta.ToString("F2")}ms. " +
                    $"Error: {error.ToString("F2")}ms. / " +
                    $"{(100.0 * error / _delay).ToString("F2")}%.");
            }
        }

        static private void TimerCallback(int id, int msg, int user, int dw1, int dw2) {
            _times.Add(DateTime.Now);
        }
    }
}
