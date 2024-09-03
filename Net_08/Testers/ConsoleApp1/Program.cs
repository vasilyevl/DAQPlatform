using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ConsoleApp1
{
    internal class Program
    {

        static void Main(string[] args) {
            Console.WriteLine("Hello, World!");
          
               // TestThreadingTimer();
                TestMultimediaTimer();
            return;
        }

        private static void TestMultimediaTimer() {

            using (var timer = new MultimediaTimer() { 

                Resolution = 0,
                Interval = 1 }) {

                timer.Elapsed += timer.TimerCallback;
                timer.Start();
                Thread.Sleep(10000);
                timer.Stop();

                Console.WriteLine($"Ave: {(MultimediaTimer.accum/(MultimediaTimer.cntr-10)).ToString("F4")}  " +
                    $"Max: {MultimediaTimer.mx.ToString("F4")}.  " +
                    $"Min: {MultimediaTimer.mn.ToString("F4")}. " +
                    $"Proc max: {MultimediaTimer.tmMax.ToString("F4")}. " +
                    $"Cntr: {MultimediaTimer.cntr - 10}");
                return;
            }
        }

        private static void TestThreadingTimer() {
            long last = 0;
            Stopwatch s = new Stopwatch();

            using (var timer = new Timer(o => {
                var el = s.ElapsedMilliseconds;
                Console.WriteLine(el - last);
                last = el;
                return;
            }))
            {
                s.Start();
                Console.ReadKey();
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
            s.Start();
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

        internal static Stopwatch s = new Stopwatch();
        internal static Stopwatch s2 = new Stopwatch();
        internal static double last = 0;
        internal static double mx = 0;
        internal static double mn = 100000;
        internal static int cntr = 0;
        internal static double accum = 0;
        internal static double tm;
        internal static double tmMax = 0;

        internal void TimerCallback (object callsr, EventArgs e) {
            s2.Restart();
            var el = s.ElapsedTicks / 10000.0;
            cntr++;
            if (cntr > 10) {
                tm = el - last;
                mx = Math.Max(mx, tm);
                mn = Math.Min(mn, tm);
                accum += tm;
            }

            last = el;

            tmMax = Math.Max(s2.ElapsedTicks / 10000.0, tmMax);
            return;
        }

        public event EventHandler<EventArgs> Elapsed;

        public void Dispose() {
            Dispose(true);
        }

        private void TimerCallbackMethod(uint id, uint msg, ref uint userCtx, uint rsv1, uint rsv2) {
            EventHandler<EventArgs> handler = Elapsed;

            TimerCallback(this, EventArgs.Empty);



          //  if (handler != null) {

                //handler.Invoke(this, EventArgs.Empty);
            //}
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
