using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HiResolution
{
    internal class Program
    {
        static void Main(string[] args) {
            int requestedPeriodMs = 1; 
            int cycles = 50;  
            Console.WriteLine($"Testing Timers with {requestedPeriodMs}ms period.");
            TestThreadingTimer(requestedPeriodMs, cycles);
            TestMultimediaTimer(requestedPeriodMs, cycles);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();  
        }

        private static void TestMultimediaTimer(int periodMs = 1, int cycles = 100) {

            Stopwatch s = new Stopwatch();
            ManualResetEvent mre = new ManualResetEvent(false);
            int i = 0;
            long prev = 0;  
            long curr = 0;
            double[] accum = new double[cycles]; 
            using (var timer = new MultimediaTimer() { Interval = periodMs }) {
                timer.Elapsed += (o, e) => { 
                    if(i == 0) {
                        curr = s.ElapsedMilliseconds;
                        prev = curr;
                    }
                    else {
                        if (i <= cycles) {
                            curr = s.ElapsedMilliseconds;
                            accum[i - 1] += (double)(curr - prev);
                            prev = curr;
                        }
                    }
                    i++;
                    if(i > cycles) {
                        mre.Set();
                    }
                };
                s.Start();
                timer.Start();
                mre.WaitOne();
                timer.Stop();
                double average = accum.Average();
                double sumOfSquaresOfDifferences = accum.Select(val => (val - average) * (val - average)).Sum();
                double sd = Math.Sqrt(sumOfSquaresOfDifferences / accum.Length);
                Console.WriteLine($"MMTimer. Average: {average}/{sd}/{accum.Max()}/{accum.Min()}. Requested: {periodMs}. Total {i-1} runs.");

            }
        }

        private static void TestThreadingTimer(int periodMs = 1, int cycles = 100) {
            Stopwatch s = new Stopwatch();
            ManualResetEvent mre = new ManualResetEvent(false);
            int i = 0;
            long prev = 0;
            long curr = 0;
            double[] accum = new double[cycles];

            using (var timer = new Timer(o => {
                if(i == 0) {
                    curr = s.ElapsedMilliseconds;
                    prev = curr;
                }
                else {
                    if (i <= cycles) {
                        curr = s.ElapsedMilliseconds;
                        accum[i - 1] = (double)(curr - prev);
                        prev = curr;
                    }
                }
                i++;
                if(i > cycles) {
                    mre.Set();
                }   
            }, null, 0, periodMs)) {
                s.Start();
                mre.WaitOne();

                double average = accum.Average();
                double sumOfSquaresOfDifferences = accum.Select(val => (val - average) * (val - average)).Sum();
                double sd = Math.Sqrt(sumOfSquaresOfDifferences / accum.Length);
                Console.WriteLine($"Threading Timer. Average: {average}/{sd}/{accum.Max()}/{accum.Min()}. Requested: {periodMs}.  Total {i-1} runs.");
            }
        }


    }

    public class MultimediaTimer : IDisposable
    {
        private bool disposed = false;
        private int interval, resolution;
        private UInt32 timerId;

        // Hold the timer callback to prevent garbage collection.
        private readonly MultimediaTimerCallback Callback;

        public MultimediaTimer() {
            Callback = new MultimediaTimerCallback(TimerCallbackMethod);
            Resolution = 5;
            Interval = 10;
        }

        ~MultimediaTimer() {
            Dispose(false);
        }

        public int Interval {
            get {
                return interval;
            }
            set {
                CheckDisposed();

                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");

                interval = value;
                if (Resolution > Interval)
                    Resolution = value;
            }
        }

        // Note minimum resolution is 0, meaning highest possible resolution.
        public int Resolution {
            get {
                return resolution;
            }
            set {
                CheckDisposed();

                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");

                resolution = value;
            }
        }

        public bool IsRunning {
            get { return timerId != 0; }
        }

        public void Start() {
            CheckDisposed();

            if (IsRunning)
                throw new InvalidOperationException("Timer is already running");

            // Event type = 0, one off event
            // Event type = 1, periodic event
            UInt32 userCtx = 0;
            timerId = NativeMethods.TimeSetEvent((uint)Interval, (uint)Resolution, Callback, ref userCtx, 1);
            if (timerId == 0) {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }
        }

        public void Stop() {
            CheckDisposed();

            if (!IsRunning)
                throw new InvalidOperationException("Timer has not been started");

            StopInternal();
        }

        private void StopInternal() {
            NativeMethods.TimeKillEvent(timerId);
            timerId = 0;
        }

        public event EventHandler Elapsed;

        public void Dispose() {
            Dispose(true);
        }

        private void TimerCallbackMethod(uint id, uint msg, ref uint userCtx, uint rsv1, uint rsv2) {
            var handler = Elapsed;
            if (handler != null) {
                handler(this, EventArgs.Empty);
            }
        }

        private void CheckDisposed() {
            if (disposed)
                throw new ObjectDisposedException("MultimediaTimer");
        }

        private void Dispose(bool disposing) {
            if (disposed)
                return;

            disposed = true;
            if (IsRunning) {
                StopInternal();
            }

            if (disposing) {
                Elapsed = null;
                GC.SuppressFinalize(this);
            }
        }
    }

    internal delegate void MultimediaTimerCallback(UInt32 id, UInt32 msg, ref UInt32 userCtx, UInt32 rsv1, UInt32 rsv2);

    internal static class NativeMethods
    {
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeSetEvent")]
        internal static extern UInt32 TimeSetEvent(UInt32 msDelay, UInt32 msResolution, MultimediaTimerCallback callback, ref UInt32 userCtx, UInt32 eventType);

        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeKillEvent")]
        internal static extern void TimeKillEvent(UInt32 uTimerId);
    }
}
