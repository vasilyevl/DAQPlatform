/*
Copyright (c) 2024 vasilyevl (Grumpy). Permission is hereby granted, 
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

namespace HighResTester
{
    using Grumpy.DaqFramework.Drivers.MMTimer;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xunit.Abstractions;
    using System.Timers;

    public class UnitTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        static TimerCaps _caps = new TimerCaps();
        uint _period = 1;
        uint _resolution = 1;

        uint _testDuration = 10000;

        double _start = 0;
        List<double> _times = new List<double>();
        List<double> _durations = new List<double>();
        double _stopWatchTicksPerMS;
        Stopwatch _stopWatch;


        public UnitTest(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
            _stopWatchTicksPerMS = Stopwatch.Frequency / 1000.0;
            _stopWatch = Stopwatch.StartNew();
        }


        [Fact]
        public void Test1_MMWrapPeriodic() {

            _testOutputHelper.WriteLine("Test1 (\"Periodic\") started.");
            Int32 r  = NativeMMTimerWrap.GetDevCaps(ref _caps,
                 Marshal.SizeOf<TimerCaps>(UnitTest._caps));

            Assert.True(r == 0, "Faile to get MMTimer caps.");

            _testOutputHelper.WriteLine("MM timer caps received: " +
                $"Min period - {_caps.PeriodMin}ms, max period - {_caps.PeriodMax}ms.");
 
            _times.Clear();

            uint resolution = Math.Max(_resolution, _caps.PeriodMin);   
            double timerConfigStart = _stopWatch.ElapsedTicks / _stopWatchTicksPerMS;

            // uint timerResolution =0;
            //  var res = NativeTimingAPI.NtSetTimerResolution(10000, true, ref timerResolution);

            // _testOutputHelper.WriteLine("System timer resolution set to " +
            //    $"{ timerResolution/10000.0}ms.");

            NativeMMTimerWrap.BeginPeriod(resolution);

            Int32 id = NativeMMTimerWrap.SetEvent(_period, 
                resolution, TimerCallback, 
                123, 
                (uint)TimerMode.Periodic);

            _start = _stopWatch.ElapsedTicks / _stopWatchTicksPerMS;
            Assert.True(id != 0, "Failed to set MM timer event.");
            _testOutputHelper.WriteLine($"MM timer confiugured in " +
                $"{(_start - timerConfigStart).ToString("F3")}ms.");

            Thread.Sleep((int)_testDuration);

            NativeMMTimerWrap.KillEvent(id);
            NativeMMTimerWrap.EndPeriod(resolution);
            ReportResults();
        }


        [Fact]
        public void Test2_MMWrap_SingleShot() {

            _testOutputHelper.WriteLine("Test2 (\"Single Shot\") started.");
            
            var r = NativeMMTimerWrap.GetDevCaps(ref _caps,
                 Marshal.SizeOf<TimerCaps>(UnitTest._caps));

            Assert.True(r == 0, "Failed to get MMTimer caps.");

            _testOutputHelper.WriteLine("MM timer caps received: \n" +
                $"Min - {_caps.PeriodMin}ms, max - {_caps.PeriodMax}ms.");
           
            for (int i = 0; i < 100; i++) {
               
                _times.Clear();

                uint resolution = Math.Max(_resolution, _caps.PeriodMin);
                NativeMMTimerWrap.BeginPeriod(resolution);

                Int32 id = NativeMMTimerWrap.SetEvent(_period,
                    resolution, TimerCallback,
                    123, (uint)TimerMode.OneShot);

                _start = _stopWatch.ElapsedTicks / _stopWatchTicksPerMS;

                Assert.True(id != 0, "Failed to set MM timer event.");
                Thread.Sleep(100);

                NativeMMTimerWrap.KillEvent(id);
                NativeMMTimerWrap.EndPeriod(resolution);

                ReportResults(i < 1);
            }
        }

        [Fact]
        public void Test3_Periodic_W_Callback() {

            _times.Clear(); 

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

            _testOutputHelper.WriteLine($"MM timer \"Test3\" complete.\n" +
                $"\tTotal processed tickes: {timer.TickCounter}.\n" +
                $"\tTotal missed ticks: {timer.MisedTickCounter}.");

            ReportResults();
        }

        [Fact]
        public void Test4_Periodic_W_Event() {

            _times.Clear();
            var timer = new HighResTimer(periodMs: _period,
                resolutionMs: _resolution,
                userCallback: null,
                operatingMode: TimerMode.Periodic,
                autoStart: false);

            _start = _stopWatch.ElapsedTicks / _stopWatchTicksPerMS;
            timer.TimerEvent += EventHandler!;
            timer.Start();

            Thread.Sleep((int)_testDuration);

            timer.Stop();

            _testOutputHelper.WriteLine($"MM timer \"Test3\" complete.\n" +
                $"\tTotal processed tickes: {timer.TickCounter}.\n" +
                $"\tTotal missed ticks: {timer.MisedTickCounter}.");

            ReportResults();
        }


        [Fact]
        public void Test5_SingleShot_W_Event() {

            _durations.Clear();

            var timer = new HighResTimer(periodMs: _period,
                resolutionMs: _resolution,
                userCallback: null,
                operatingMode: TimerMode.OneShot,
                autoStart: false);

            
            timer.TimerEvent += SingleSHutEventHandler!;
            
            for( int i = 0; i < 50; i++) {

                _start = _stopWatch.ElapsedTicks / _stopWatchTicksPerMS;
                timer.Start();
                Thread.Sleep((int)_period*2); 
            }

            _testOutputHelper.WriteLine($"MM timer \"Test5\" complete.\n" +
                $"\tTotal processed tickes: {_times.Count}.");

            ReportSingleShotResults();
        }

        [Fact]
        public void Test6_Wait() {

            _durations.Clear();
            var accum = 0.0;
            var timer = new HighResTimer();
            
            _testOutputHelper.WriteLine($"##\tWait ms\tError ms\tError %");

            for (int i = 0; i < 50; i++) {

                var r = timer.Wait(_period);
                _start = timer.StartTimeMs;
                _durations.Add(timer.DurationMs);

                accum += _durations[i];
                
                Assert.True(r, "Failed to wait for timer.");

                _testOutputHelper.WriteLine($"{_durations.Count}" +
                    $"\t{_durations[i].ToString("F2")}" +
                    $"\t{(_durations[i] - _period).ToString("F3")}"+
                    $"\t{(100.0 * (_durations[i] - _period) / _period).ToString("F2")}%");
            }

            _testOutputHelper.WriteLine($"MM timer \"Test6\" complete.\n" +
                $"\tTotal processed tickes: {_times.Count}.");

            _testOutputHelper.WriteLine($"Average duration: " +
                $"{(accum / _durations.Count).ToString("F2")}ms.");
        }


        [Fact]
        public void Test7_StopWatch() {

            _times.Clear();
            _start = _stopWatch.ElapsedTicks / _stopWatchTicksPerMS;

            for(int i = 0; i < _testDuration; i++) {

                _times.Add(_stopWatch.ElapsedTicks / _stopWatchTicksPerMS);
            }


            ReportTimes();
        }


        [Fact]
        public void Test8_Periodic_SystemTimer() {
             
            NativeMMTimerWrap.BeginPeriod(1);
            _times.Clear();
            _start = _stopWatch.ElapsedTicks / _stopWatchTicksPerMS;

            var aTimer = new System.Timers.Timer(20);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;

            Thread.Sleep((int)_testDuration);

            _testOutputHelper.WriteLine($"MM timer \"Test8\" complete.\n");

            ReportResults();
        }

        private void TimerCallback(int id, int msg, int user, int dw1, int dw2) {
            _times.Add(_stopWatch.ElapsedTicks / _stopWatchTicksPerMS);
        }

        private void TimerProc(int timerID, ulong clicks, double time) {

            _times.Add(time);
        }

        private void EventHandler(object sender, TimerEventArgs e) {
            _times.Add(e.Time);
        }

        private void SingleSHutEventHandler(object sender, TimerEventArgs e) {
           
            _durations.Add((_stopWatch.ElapsedTicks / _stopWatchTicksPerMS) - _start);
        }

        private void ReportResults( bool showHeader = true) {

            if (showHeader) {
                _testOutputHelper.WriteLine($"Start Time: {_start.ToString()}ms.");
                _testOutputHelper.WriteLine($"Period: {_period}ms");

                _testOutputHelper.WriteLine($" ## \tTime \tDelta.\tError ms.\tError %");
            }
            for (int i = 0; i < _times.Count; i++) {
                Double delta = (_times[i] - ((i == 0) ? _start: _times[i - 1]));
                var error = delta - _period;
                _testOutputHelper.WriteLine($" {i+1}." +
                    $"{(i < 10  ? " " :"")}"+
                    $"\t{(_times[i] - _start)}" +
                    $"\t{(delta >=0 ? " " : "")}" +
                    $"{delta.ToString("F3")}" +
                    $"\t   {(delta >=0 ? " " : "")}" +
                    $"{error.ToString("F3")}" +
                    $"\t{(delta >= 0 ? " " : "")}" +
                    $"{(100.0 * error / _period).ToString("F2")}");
            }
        }

        private void ReportTimes() {


            for (int i = 0; i < _times.Count; i++) {
                Double delta = (_times[i] - ((i == 0) ? _start : _times[i - 1]));
                var error = delta - _period;
                _testOutputHelper.WriteLine($" {i + 1}." +
                    $"{(i < 10 ? " " : "")}" +
                    $"\t{_times[i]}");
            }
        }

        private void ReportSingleShotResults() {

            _testOutputHelper.WriteLine($"Period: {_period}ms");
            var accum = 0.0;
            for (int i = 0; i < _durations.Count; i++) {

                _testOutputHelper.WriteLine($" #{i + 1}. \tTime delta: " +
                    $"{_durations[i].ToString("F2")}ms."); 
            
                accum += _durations[i];
            }

            _testOutputHelper.WriteLine($"Average duration: {(accum / _durations.Count).ToString("F2")}ms.");
        }

        private void OnTimedEvent(Object? source, ElapsedEventArgs e) {

            _times.Add(_stopWatch.ElapsedTicks / _stopWatchTicksPerMS);
        }
    }
}