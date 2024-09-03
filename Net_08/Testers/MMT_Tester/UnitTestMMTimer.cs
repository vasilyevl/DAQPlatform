namespace MMT_Tester
{
    using Grumpy.Utilities.MMTimer;
    using System.Runtime.InteropServices;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public class UnitTestMMTimer
    {
        private readonly ITestOutputHelper _testOutputHelper;
        static TimerCaps _caps = new TimerCaps();
        uint _delay = 1;
        uint _resolution = 1;
        DateTime _start = DateTime.Now;
        List<DateTime> _times = new List<DateTime>();
        public UnitTestMMTimer(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
        }

        uint _testDuration = 10000;


        [Fact]
        public void Test1MMTWrapPeriodic() {

            _testOutputHelper.WriteLine("Test1 (\"Periodic\") started.");
            Int32 r  = NativeMMTimerWrap.GetDevCaps(ref _caps,
                 Marshal.SizeOf<TimerCaps>(UnitTestMMTimer._caps));

            Assert.True(r == 0, "Failed to get MMTimer caps.");

            _testOutputHelper.WriteLine("MM timer caps received: \n" +
                $"Min - {_caps.PeriodMin}ms, max - {_caps.PeriodMax}ms.");

            _start = DateTime.Now;
 
            _times.Clear();

            uint resolution = Math.Max(_resolution, _caps.PeriodMin);   
            NativeMMTimerWrap.BeginPeriod(resolution);

            Int32 id = NativeMMTimerWrap.SetEvent(_delay, 
                resolution, TimerCallback, 
                123, 
                (uint)TimerMode.Periodic);

            Assert.True(id != 0, "Failed to set MM timer event.");
            Thread.Sleep((int)_testDuration);

            NativeMMTimerWrap.KillEvent(id);
            NativeMMTimerWrap.EndPeriod(resolution);
            ReportResults();
        }


        [Fact]
        public void Test2MMTWrapSingleShot() {
            _testOutputHelper.WriteLine("Test2 (\"Single Shot\") started.");
            var r = NativeMMTimerWrap.GetDevCaps(ref _caps,
                 Marshal.SizeOf<TimerCaps>(UnitTestMMTimer._caps));

            Assert.True(r == 0, "Failed to get MMTimer caps.");


            _testOutputHelper.WriteLine("MM timer caps received: \n" +
                $"Min - {_caps.PeriodMin}ms, max - {_caps.PeriodMax}ms.");

            for (int i = 0; i < 100; i++) {

                _start = DateTime.Now;

                _times.Clear();

                uint resolution = Math.Max(_resolution, _caps.PeriodMin);
                NativeMMTimerWrap.BeginPeriod(resolution);
                Int32 id = NativeMMTimerWrap.SetEvent(_delay,
                    resolution, TimerCallback,
                    123,
                    (uint)TimerMode.OneShot);

                Assert.True(id != 0, "Failed to set MM timer event.");
                Thread.Sleep(100);

                NativeMMTimerWrap.KillEvent(id);
                NativeMMTimerWrap.EndPeriod(resolution);

                ReportResults();
            }
        }

        [Fact]
        public void Test3MMTimerPeriodic() {

            _times.Clear(); 
            var timer = new MMTimer( periodMs: 1, 
                resolutionMs:1, 
                userCallback: TimerProc, 
                operatingMode: TimerMode.Periodic, 
                autoStart: false);

            _start = DateTime.Now;
            
            timer.Start();
            
            Thread.Sleep((int)_testDuration);
            
            timer.Stop();

            _testOutputHelper.WriteLine($"MM timer \"Test3\" complete.\n" +
                $"\tTotal processed tickes: {timer.TickCounter}.\n" +
                $"\tTotal missed ticks: {timer.MisedTickCounter}.");

            ReportResults();
        }





        private void TimerCallback(int id, int msg, int user, int dw1, int dw2) {
            _times.Add(DateTime.Now);
        }


        private void TimerProc(int timerID, ulong clicks, DateTime time) {

            _times.Add(time);

        }


        private void ReportResults(){

            _testOutputHelper.WriteLine($"Start Time: {_start.ToString("HH:mm:ss.fff")}");
            _testOutputHelper.WriteLine($"Period: {_delay}ms");
            for (int i = 0; i < _times.Count; i++) {
                var delta = (_times[i] - ((i == 0) ? _start : _times[i - 1])).TotalMilliseconds;
                var error = delta - _delay;
                _testOutputHelper.WriteLine($"Time: {_times[i].ToString("HH:mm:ss.fff")}, delta: " +
                    $"{delta.ToString("F2")}ms. Error: {error.ToString("F2")}ms. /  {(100.0 * error / _delay).ToString("F2")}%.");
            }
        }
    }
}