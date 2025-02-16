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

using Microsoft.Extensions.Logging;

namespace Grumpy.StatePatternFramework
{

    public class StateWorker
    {
        protected ILogger? _logger = null;

        private Thread? thread;
        private AutoResetEvent? resetEvent;
        private CancellationTokenSource cts;

        private readonly object workerLock = new();

        public StateWorker(ILogger? logger = null) {
            _logger = logger;
            cts = new CancellationTokenSource();
            resetEvent = new AutoResetEvent(false);

        }

        public void Start(Action<CancellationToken> workerLogic) {
            lock (workerLock) {
                if (thread != null) return;
                thread = new Thread(() => workerLogic(cts.Token));
                thread.Start();
            }
        }

        public void StopWorkerThread() {
            lock (workerLock) {
                cts?.Cancel();
                thread?.Join();
                thread = null;
            }
        }

        public bool PauseWorker() {
            lock (workerLock) {

                if (thread == null) return false;

                resetEvent?.Reset();
                return true;
            }
        }

        public bool ResumeWorker() {
            lock (workerLock) {

                if ((thread == null)
                    || (thread.ThreadState == ThreadState.Unstarted)) {

                    _logger?.LogWarning($"State Machine worker: " +
                        $"an attempt to resume nonexisting or " +
                        $"unstarted worker thread.");

                    return false;
                }

                if (thread.ThreadState == ThreadState.WaitSleepJoin) {

                    resetEvent?.Set();
                }

                return true;
            }
        }

        public bool IsRunning => thread?.IsAlive ?? false;

        public bool IsPaused =>
            (thread != null) && thread.ThreadState == ThreadState.WaitSleepJoin;

        public bool IsSuspended =>
            (thread != null) && thread.ThreadState == ThreadState.Suspended;
    }
}

