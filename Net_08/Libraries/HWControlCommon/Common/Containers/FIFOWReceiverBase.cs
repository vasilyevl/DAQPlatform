/*
Copyright (c) 2025 vasilyevl (Grumpy). Permission is hereby granted, 
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

namespace Grumpy.SDAQFramework.Common
{
    public enum ReceiverState
    {
        NotSet,
        Running,
        Paused,
        Stopped
    }

    public delegate void DataReceiver<TObject>(TObject obj);

    public class FIFOWReceiverBase<TObject> : FIFOBase<TObject>
    {
        
        private DataReceiver<TObject>? _receiver;
        private Task? _receiverTask;
        private readonly object _receiverTaskLock;
        private AutoResetEvent? _receiverTaskAutoEvent;
        private bool _isReceiverPaused;
        private bool _usePause;
        private bool _internallyPaused;
        private CancellationTokenSource? _receiverTaskCts;
        private CancellationToken _processorTaskCt;
  
        public event EventHandler? ReceiverStartedOrResumed;

        public FIFOWReceiverBase(int capacity) : base(capacity) {

            _receiverTaskLock = new object();
        }

        public FIFOWReceiverBase() : base() {

            _receiverTaskLock = new object();
        }

        public bool ReceiverIsSet => _receiver != null;

        public bool ReceiverIsOn => _receiver != null
            && ReceiverThreadAlive
            && !ReceiverIsPaused;

        public bool ReceiverThreadAlive =>
            _receiverTask != null &&
            (_receiverTask.Status == TaskStatus.Running ||
             _receiverTask.Status == TaskStatus.WaitingForActivation ||
             _receiverTask.Status == TaskStatus.WaitingToRun);

        public bool ReceiverIsInternallyPaused {
            get {

                lock (_receiverTaskLock) {

                    return _internallyPaused;
                }
            }
            private set {

                lock (_receiverTaskLock) {

                    _internallyPaused = value;
                }
            }
        }

        public bool ReceiverIsPaused {
            get {

                lock (_receiverTaskLock) {

                    return _isReceiverPaused;
                }
            }
            protected set {

                lock (_receiverTaskLock) {

                    _isReceiverPaused = value;
                }
            }
        }

        public bool UsePause {

            get {

                lock (_receiverTaskLock) {

                    return _usePause;
                }
            }
            protected set {

                lock (_receiverTaskLock) {

                    _usePause = value;
                }
            }
        }

        public bool SetReceiver(DataReceiver<TObject> processor) {

            if (_receiverTask == null ||
                _receiverTask.Status == TaskStatus.RanToCompletion ||
                _receiverTask.Status == TaskStatus.Faulted ||
                _receiverTask.Status == TaskStatus.Canceled) {

                _receiver = processor;
                return true;
            }

            return false;
        }

        public bool PauseReceiver() {

            lock (_receiverTaskLock) {

                if (ReceiverIsPaused) {

                    ReceiverIsInternallyPaused = false;
                    return true;
                }

                if ((_receiverTask?.Status ?? TaskStatus.RanToCompletion)
                    == TaskStatus.Running) {

                    _receiverTaskAutoEvent = new AutoResetEvent(false);
                    ReceiverIsPaused = true;
                    ReceiverIsInternallyPaused = false;
                    return true;
                }
            }

            return false;
        }

        public bool ResumeReceiver() {

            if (_receiverTask == null && _receiver != null) {

                if (!_ActivateReceiver()) {

                    return false;
                }
            }

            if (!ReceiverIsPaused) {

                return true;
            }
            else {

                if (_receiver != null &&
                    _receiverTask != null &&
                    _receiverTask.Status == TaskStatus.Running) {

                    if (_receiverTaskAutoEvent != null) {

                        _receiverTaskAutoEvent.Set();
                        ReceiverStartedOrResumed?.Invoke(this, EventArgs.Empty);
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool Push(TObject item, out string err) {

            bool wasEmpty = IsEmpty;

            if (base.Push(item, out err)) {

                return ResumeReceiver();
            }
            else {

                return false;
            }
        }

        protected override void DisposeManagedResources() {

            if (ReceiverThreadAlive) {

                try {
                    _receiverTaskCts?.Cancel();

                    if (!ReceiverIsPaused) {

                        PauseReceiver();
                    }
                }
                catch (Exception _) {
                    // Log error
                }
            }
        }

        private bool _ActivateReceiver() {

            if (_receiverTask == null) {

                if (_receiver != null) {

                    _StartReceiverTask();
                }
            }
            else {

                switch (_receiverTask.Status) {

                    case TaskStatus.Running:
                        if (UsePause && ReceiverIsPaused && ReceiverIsInternallyPaused) {
                            ResumeReceiver();
                        }
                        break;

                    case TaskStatus.WaitingForActivation:
                    case TaskStatus.WaitingToRun:
                        break;

                    case TaskStatus.RanToCompletion:
                    case TaskStatus.Canceled:
                    case TaskStatus.Faulted:
                        if (_receiver != null) {
                            try {
                                _receiverTask?.Dispose();
                            }
                            catch (InvalidOperationException _) {
                                // Log warning
                            }
                            _receiverTask = null;
                            _StartReceiverTask();
                        }
                        break;

                    default:
                        // Log warning
                        break;
                }
            }

            return _receiverTask != null;
        }

        private bool _PauseReceiver() {

            lock (_receiverTaskLock) {

                if (ReceiverIsPaused) {

                    ReceiverIsInternallyPaused = true;
                    return true;
                }

                if ((_receiverTask?.Status ?? TaskStatus.Created) == TaskStatus.Running) {

                    _receiverTaskAutoEvent = new AutoResetEvent(false);
                    ReceiverIsPaused = true;
                    ReceiverIsInternallyPaused = true;
                    return true;
                }
            }
            return false;
        }

        private void _ReceiverWorker() {

            while (!_processorTaskCt.IsCancellationRequested) {

                if (Count < 1) {

                    if (UsePause) {

                        _PauseReceiver();
                    }
                    else {
                        break;
                    }
                }

                if (_receiverTaskAutoEvent != null) {

                    if (ReceiverIsInternallyPaused) {

                        // Raise state change event
                    }

                    _receiverTaskAutoEvent.WaitOne();
                    _receiverTaskAutoEvent = null;

                    if (!ReceiverIsInternallyPaused) {

                        // Raise state change event
                    }

                    ReceiverIsPaused = false;
                    ReceiverIsInternallyPaused = false;
                }

                while (Count > 0) {

                    if (Pop(out TObject item, out string err)) {

                        _receiver?.Invoke(item);
                    }
                }
            }
            _Cleanup();
        }

        private void _Cleanup() {

            lock (_receiverTaskLock) {

                // Raise state change event
                _receiverTaskAutoEvent = null;
                _receiverTaskCts = null;
                _processorTaskCt = default;
                _receiverTask = null;
            }
        }

        private void _StartReceiverTask() {

            if (_receiverTask == null) {

                lock (_receiverTaskLock) {

                    _receiverTaskCts = new CancellationTokenSource();
                    _processorTaskCt = _receiverTaskCts.Token;
                    _receiverTask = new Task(_ReceiverWorker, TaskCreationOptions.LongRunning);
                    _receiverTask.Start();
                    
                }

                ReceiverStartedOrResumed?.BeginInvoke(this, EventArgs.Empty, null, null);
            }
        }
    }
}