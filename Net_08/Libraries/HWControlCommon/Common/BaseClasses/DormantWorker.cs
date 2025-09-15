using System;
using System.Threading;
using System.Runtime.CompilerServices;
namespace Grumpy.SDAQFramework.Common
{
    /// <summary>
    /// BackgroundWorker runs a long-running Action in a dedicated thread, supporting pause/resume and disposal.
    /// After each action execution, the thread pauses until Resume() is called.
    /// </summary>
    public class DormantWorker : IDisposable
    {
        private const int _DefaultTimeoutMs = 10000;

        private readonly Thread _workerThread;
        private readonly Action _workAction;
        private readonly ManualResetEventSlim _pauseEvent; 
        private readonly CancellationTokenSource _cts;
        private bool _disposed;
        private readonly int _timeout;
        private readonly object _syncRoot;
        private bool _starting;
        private bool _snoosing;
        private bool _paused;
        public DormantWorker(Action workAction, 
            int timeout = _DefaultTimeoutMs, 
            bool paused  = false) {

            _pauseEvent = new(false);   // Start paused
            _cts = new();
            _starting = true;
            _syncRoot = new();
            _workAction = workAction ?? 
                throw new ArgumentNullException(nameof(workAction));
            _timeout = timeout;
            _snoosing = false; // Initially not dormant
            _paused = paused;  // Initially not paused by default.

            _workerThread = new Thread(Worker) {

                IsBackground = true
            };

            _workerThread.Start();
        }

        private void Snoose() {

            if (_snoosing) {
                Console.WriteLine("[BackgroundWorker] Already in " +
                    "dormant state, no action taken.");
                return;
            }

            Console.WriteLine("[BackgroundWorker] Worker is " +
                "switching to dormant.");
            // make sure the event is reset before waiting just in case.
            _pauseEvent.Reset(); 
            _snoosing = true;
            _pauseEvent.Wait(_cts.Token); // Wait for WakeUp() to be called
            // Insurwe _pauseEvent can be used again,
            _pauseEvent.Reset(); // Ensure worker is paused
            _snoosing = false;
            Console.WriteLine("[BackgroundWorker] Worker resumed " +
                "from dormant state.");
        }

        private void Worker() {

            if (_starting) {

                Console.WriteLine("[BackgroundWorker] Thread just started. ");
                _starting = false;
                Snoose();
            }
            
            if(!_cts.IsCancellationRequested) {

                if (!_paused) {
                    try {
                        Console.WriteLine("[BackgroundWorker] Executing " +
                            "work action.");
                        _workAction();
                    }
                    catch (OperationCanceledException) {
                        return;
                    }
                    catch {
                        // Swallow exceptions to keep the thread alive
                    }
                }
                Snoose();
            }
            else {
                // If cancellation is requested, exit the thread
                Console.WriteLine("[BackgroundWorker] Cancellation " +
                    "requested, exiting worker thread.");
                return;
            }
        }

        /// <summary>
        /// Resumes the background worker (allows one action execution).
        /// </summary>
        public void WakeUp() {
            lock (_syncRoot) {
                _WakeUp();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _WakeUp() => _pauseEvent.Set();


        /// <summary>
        /// Pauses the background worker (no-op, as worker pauses itself after each action).
        /// </summary>
        public bool Paused {
            get {
                lock (_syncRoot) {
                    return _paused;
                }
            }
            set {
                lock (_syncRoot) {
                    if (_paused != value) {

                        if (value == false && _snoosing) {
                            _paused = value;
                            _WakeUp(); // Cannot unpause while dormant
                            return;
                        }
                        else {

                        }
                    }
                    _paused = value;
                    if (_paused ) {
                        Snoose(); // Pause the worker
                    }
                    else {
                        _pauseEvent.Set(); // Resume the worker
                    }
                }
            }
        }

        /// <summary>
        /// Stops the background worker and releases resources.
        /// </summary>
        public void Dispose() {

            if (_disposed) {
            
                return; 
            }

            _disposed = true;
            _cts.Cancel();

            lock (_syncRoot) {

                _pauseEvent.Set(); // Ensure thread can exit if paused
            }

            if (_workerThread.IsAlive) {

                if (!_workerThread.Join(_timeout)) {

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[BackgroundWorker] Worker thread " +
                        $"did not stop within {_timeout}ms and may be stuck.");
                    Console.ResetColor();
                }
            }
            _pauseEvent.Dispose();
            _cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}