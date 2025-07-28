using System;
using System.Threading;

namespace Grumpy.Common.BaseObjects
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
        private bool _dormant;
        public DormantWorker(Action workAction, int timeout = _DefaultTimeoutMs) {

            _pauseEvent = new(false);   // Start paused
            _cts = new();
            _starting = true;
            _syncRoot = new();
            _workAction = workAction ?? throw new ArgumentNullException(nameof(workAction));
            _timeout = timeout;
            _dormant = false; // Initially not dormant

            _workerThread = new Thread(Worker) {

                IsBackground = true
            };

            _workerThread.Start();
        }

        private void PutToDormantState() {

            if (_dormant) {

                Console.WriteLine("[BackgroundWorker] Already in " +
                    "dormant state, no action taken.");
                return;
            }

            Console.WriteLine("[BackgroundWorker] Worker is switching to dormant.");
            _dormant = true;
            _pauseEvent.Wait(_cts.Token); // Wait for Resume() to be called
            _pauseEvent.Reset(); // Ensure worker is paused
            _dormant = false;
            Console.WriteLine("[BackgroundWorker] Worker resumed from dormant state.");
        }

        private void Worker() {

            if (_starting) {

                Console.WriteLine("[BackgroundWorker] Thread just started. ");
                _starting = false;
                PutToDormantState();
            }
            
            if(!_cts.IsCancellationRequested) {

                try {
                    Console.WriteLine("[BackgroundWorker] Executing work action.");
                    _workAction();
                }
                catch (OperationCanceledException) {
                    return;
                }
                catch {
                    // Swallow exceptions to keep the thread alive
                }

                PutToDormantState();
            }
            else {
                // If cancellation is requested, exit the thread
                Console.WriteLine("[BackgroundWorker] Cancellation requested, exiting worker thread.");
                return;
            }
        }

        /// <summary>
        /// Resumes the background worker (allows one action execution).
        /// </summary>
        public void Resume() {
            lock (_syncRoot) {
                _pauseEvent.Set();
            }
        }

        /// <summary>
        /// Pauses the background worker (no-op, as worker pauses itself after each action).
        /// </summary>
        public void Pause() {
            lock (_syncRoot) {
                PutToDormantState();
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
                    Console.WriteLine($"[BackgroundWorker] Thread did not stop within {_timeout} ms and may be stuck.");
                    Console.ResetColor();
                }
            }
            _pauseEvent.Dispose();
            _cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}