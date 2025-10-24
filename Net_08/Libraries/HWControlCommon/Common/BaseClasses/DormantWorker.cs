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
        public static bool EnableConsoleLogging { get; set; } = false;

        private const int _DefaultTimeoutMs = 10000;

        private readonly Thread _workerThread;
        private readonly Action _action;
        private readonly ManualResetEventSlim _triggerEvent; 
        private readonly CancellationTokenSource _cts;
        private bool _disposed;
        private readonly int _timeout;
        private readonly object _syncRoot;
        private bool _isStarting;
        private bool _isWaitingForTrigger;
        private bool _paused;
        private readonly ManualResetEventSlim _initializedEvent;

        public DormantWorker(Action action,
            int timeout = _DefaultTimeoutMs,
            bool paused = false,
            bool waitWhenReady = true) {
            _initializedEvent = new(false);
            _triggerEvent = new(false);   // Start paused
            _cts = new();
            _isStarting = true;
            _syncRoot = new();
            _action = action ??
                throw new ArgumentNullException(nameof(action));
            _timeout = timeout;
            _isWaitingForTrigger = false; // Initially not dormant
            _paused = paused;  // Initially not paused by default.

            _workerThread = new Thread(Worker) {

                IsBackground = true
            };

            _workerThread.Start();

            if (waitWhenReady) {
            
                if (!_initializedEvent.Wait(_DefaultTimeoutMs)) // Wait up to 10s.
                {
                    throw new TimeoutException("Worker did not signal readiness in time.");
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the worker thread has signaled readiness.
        /// </summary>
        public bool IsInitialized {
            get {
                // ManualResetEventSlim is thread-safe for WaitHandle state queries.
                return _initializedEvent.IsSet;
            }
        }

        public bool IsDormant {

            get {

                lock (_syncRoot) {
                    return _isWaitingForTrigger;
                }
            }
            private set {
                lock (_syncRoot) {
                    _isWaitingForTrigger = value;
                }
            }
        }

        private void Snoose() {

            if (IsDormant) {
                if (EnableConsoleLogging)
                    Console.WriteLine("[DormantWorker] Already in dormant state, no action taken.");
                return;
            }

            if (EnableConsoleLogging)
                Console.WriteLine("[DormantWorker] Worker is switching to dormant.");
            _triggerEvent.Reset();
            IsDormant = true;
            _triggerEvent.Wait(_cts.Token); // Wait for Trigger() to be called
            _triggerEvent.Reset(); // Ensure worker is paused
            IsDormant = false;
            if (EnableConsoleLogging)
                Console.WriteLine("[DormantWorker] Worker resumed from dormant state.");
        }

        private void Worker() {

            if (_isStarting) {
                if (EnableConsoleLogging)
                    Console.WriteLine("[DormantWorker] Thread just started.");
                _isStarting = false;
                _initializedEvent.Set();
                Snoose();
            }

            while (!_cts.IsCancellationRequested) {

                if (!_paused) {
                    try {
                        if (EnableConsoleLogging)
                            Console.WriteLine("[DormantWorker] Executing work action.");
                        _action();
                    }
                    catch (OperationCanceledException e) {
                        if (EnableConsoleLogging)
                            Console.WriteLine($"[DormantWorker] Exception triggered {e}");
                        return;
                    }
                    catch (Exception e) {
                        if (EnableConsoleLogging)
                            Console.WriteLine($"[DormantWorker] Exception triggered {e}");
                        // Swallow exceptions to keep the thread alive
                    }
                }
                else {
                    if (EnableConsoleLogging)
                        Console.WriteLine("[DormantWorker] Paused.");
                }
                Snoose();
            }

            if (EnableConsoleLogging)
                Console.WriteLine("[DormantWorker] Cancellation requested, exiting worker thread.");
            return;
        }


        public bool Trigger() {
            lock (_syncRoot) {
                return _Trigger();
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool _Trigger() {

            if (_initializedEvent.IsSet == false) {
                return false; // Not initialized yet
            }
            if (EnableConsoleLogging)
                Console.WriteLine("[DormantWorker] _Trigger() called.");
            if (!_triggerEvent.IsSet) {
                if (EnableConsoleLogging)
                    Console.WriteLine("[DormantWorker] Setting _triggerEvent");
                _triggerEvent.Set();
            }
            else {
                if (EnableConsoleLogging)
                    Console.WriteLine("[DormantWorker] _triggerEvent is already set.");
            }
            return true;
        }

  

        /// <summary>
        /// Pauses the background worker (no-op, as worker pauses itself after each action).
        /// </summary>
        public bool IsPaused {
            get {
                lock (_syncRoot) {
                    return _paused;
                }
            }
            set {
                lock (_syncRoot) {
                    if (_paused != value) {

                        if (value == false && _isWaitingForTrigger) {
                            _paused = value;
                            _Trigger(); // Cannot unpause while dormant
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
                        _triggerEvent.Set(); // Resume the worker
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
                _triggerEvent.Set(); // Ensure thread can exit if paused
            }

            if (_workerThread.IsAlive) {
                if (!_workerThread.Join(_timeout)) {
                    if (EnableConsoleLogging) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[DormantWorker] Worker thread did not stop within {_timeout}ms and may be stuck.");
                        Console.ResetColor();
                    }
                }
            }
            _triggerEvent.Dispose();
            _cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}