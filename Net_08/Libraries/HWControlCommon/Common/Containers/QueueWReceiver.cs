using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grumpy.Common
{
    public delegate void DataReceiver<TObject>(TObject obj);

    public class QueueWReceiver<TObject> : BasicQueue<TObject>
    {
        private DataReceiver<TObject>? _receiver;

        private Task? _receiverTask;
        private object _receiverTaskLock;
        private AutoResetEvent? _receiverTaskAutoEvent;
        private bool _isReceiverPaused;
        private bool _usePause;
        private bool _internallyPaused;

        private CancellationTokenSource? _receiverTaskCts;
        private CancellationToken _processorTaskCt;

        public QueueWReceiver( string? name = null, int maxDepth = DefaultMaxCapacity, 
            DataReceiver<TObject>? receiver = null, bool syncEvents = true ) : 
                base(maxDepth: DefaultMaxCapacity, name: null, syncEvents: true)
        {
            _receiverTaskCts = new CancellationTokenSource();
            _receiverTaskLock = new object();
            _receiver = receiver;
            _receiverTask = null;
        }

        public bool ReceiverIsSet => _receiver != null;

        public bool ReceiverIsOn =>
                (_receiver != null) && ReceiverThreadAlive && (!ReceiverIsPaused);

        public bool ReceiverThreadAlive =>
                   (_receiverTask != null) &&
                   ((_receiverTask.Status == TaskStatus.Running) ||
                   (_receiverTask.Status == TaskStatus.WaitingForActivation) ||
                   (_receiverTask.Status == TaskStatus.WaitingToRun));

        public bool ReceiverIsInternallyPaused {
            get {
                bool result;
                Thread.MemoryBarrier();
                result = _internallyPaused;
                Thread.MemoryBarrier();
                return result;
            }
            private set {
                bool setValue = value;
                Thread.MemoryBarrier();
                _internallyPaused = setValue;
                Thread.MemoryBarrier();
            }
        }

        public bool ReceiverIsPaused {
            get {
                bool ret;
                Thread.MemoryBarrier();
                ret = _isReceiverPaused;
                Thread.MemoryBarrier();
                return ret;
            }
            protected set {
                Thread.MemoryBarrier();
                _isReceiverPaused = value;
                Thread.MemoryBarrier();
            }
        }

        public bool UsePause {
            get {
                bool ret;
                Thread.MemoryBarrier();
                ret = _usePause;
                Thread.MemoryBarrier();
                return ret;
            }
            protected set {
                Thread.MemoryBarrier();
                _usePause = value;
                Thread.MemoryBarrier();
            }
        }

        public override void Dispose()
        {
            if (ReceiverThreadAlive) {

                try {
                    _receiverTaskCts.Cancel();

                    if (ReceiverIsPaused) {
                        ResumeReceiver();
                    }

                    if (!(_receiverTask?.Wait(WorkerCancellationTimeoutMs) ?? false)) {

                        LastError = ($"ConsumerQueue. Dispose(). " +
                            $"Queue - {Name}. " +
                            $"Failed to cancel worker task within " +
                            $"{WorkerCancellationTimeoutMs} ms.");
                    }
                }
                catch (Exception ex) {

                    LastError = ($"ConsumerQueue. Dispose(). " +
                        $"Queue - {Name}. " +
                    $"Exception {ex.Message}");
                }
            }
            base.Dispose();
        }

        public bool SetReceiver(DataReceiver<TObject> processor)
        {
            if ((_receiverTask == null) ||
                (_receiverTask.Status == TaskStatus.RanToCompletion) ||
                (_receiverTask.Status == TaskStatus.Faulted) ||
                (_receiverTask.Status == TaskStatus.Canceled)) {

                _receiver = processor;
                return true;
            }
            LastError = $"Queue {Name}. Can't set consumer processor. " +
                             $"Processing task is active.";
            return false;
        }

        public bool PauseReceiver()
        {
            lock (_receiverTaskLock) {

                if (ReceiverIsPaused) {

                    ReceiverIsInternallyPaused = false;
                    return true;
                }

                if (_receiverTask.Status == TaskStatus.Running) {

                    _receiverTaskAutoEvent = new AutoResetEvent(false);
                    ReceiverIsPaused = true;
                    ReceiverIsInternallyPaused = false;
                    return true;
                }
            }
            return false;
        }

        public bool ResumeReceiver()
        {
            if (ReceiverIsPaused) {

                if ((_receiver != null) &&
                     (_receiverTask != null) &&
                     (_receiverTask.Status == TaskStatus.Running)) {

                    if (_receiverTaskAutoEvent != null) {

                        _receiverTaskAutoEvent.Set();
                        return true;
                    }
                }
            }
            return false;
        }

        private void _CancellReceiverThread()
        {
            if (_receiver != null) {

                lock (_receiverTaskLock) {

                    if ((_receiverTask != null) &&
                        ((_receiverTask.Status == TaskStatus.Running) ||
                          (_receiverTask.Status == TaskStatus.Created) ||
                          (_receiverTask.Status == TaskStatus.WaitingForActivation))) {

                        _receiverTaskCts.Cancel();
                    }
                }
            }
            ReceiverIsPaused = false;
        }


        public override bool Push(TObject obj, bool force = false)
        {
            lock(_queueLock) {

                 if(base._Push(obj, force)) {

                    _ActivateReceiver();
                    return true;
                 }

                return false;
            }
        }

        private bool _ActivateReceiver()
        {
            if (_receiverTask == null) {

                if (_receiver != null) {
                    _StartReceiverTask();
                }
            }
            else {
                switch (_receiverTask.Status) {

                    case (TaskStatus.Running):
                        if (UsePause && ReceiverIsPaused && ReceiverIsInternallyPaused) {
                            ResumeReceiver();
                        }
                        break;                       

                    case (TaskStatus.WaitingForActivation):
                    case (TaskStatus.WaitingToRun):

                        break;

                    case (TaskStatus.RanToCompletion):
                    case (TaskStatus.Canceled):
                    case (TaskStatus.Faulted):
                        if (_receiver != null) {

                            try {
                                if (_receiverTask != null) {
                                    _receiverTask.Dispose();
                                }
                            }
                            catch (InvalidOperationException ex) {
                                LastError = $"Coonsumer queue {Name} " +
                                    $"exception on processor task " +
                                    $"dispose attempt: {ex.Message}.";
                            }
                            _receiverTask = null;
                            _StartReceiverTask();
                        }
                        break;
                    
                    default:
                        LastError = $"Active queue {Name}. _ActivateReceiver(). " +
                            $"Unexpected thread status {_receiverTask.Status}";
                        break;
                }
            }
            return _receiverTask != null;
        }

        private bool _PauseReceiver()
        {
            lock (_receiverTaskLock) {

                if (ReceiverIsPaused) {

                    ReceiverIsInternallyPaused = true;
                    return true;
                }

                if (_receiverTask.Status == TaskStatus.Running) {

                    _receiverTaskAutoEvent = new AutoResetEvent(false);
                    ReceiverIsPaused = true;
                    ReceiverIsInternallyPaused = true;
                    return true;
                }
            }

            return false;
        }

        private void _ReceiverWorker()
        {
            while (!_processorTaskCt.IsCancellationRequested) {

                // Queue is empty. Let's pause the thread. 
                if (Count < 1) {

                    if (UsePause) {
                        _PauseReceiver();
                    }
                    else {
                        break;
                    }
                }

                // 
                if (_receiverTaskAutoEvent != null) {

                    if (ReceiverIsInternallyPaused) {
                        _RaiseStateChangeEvent(Events.ProcessorTaskPaused);
                    }
                    // Wait until task resumed.
                    _receiverTaskAutoEvent.WaitOne();
                    _receiverTaskAutoEvent = null;

                    if (!ReceiverIsInternallyPaused) {
                        _RaiseStateChangeEvent(Events.ProcessorTaskResumed);
                    }

                    ReceiverIsPaused = false;
                    ReceiverIsInternallyPaused = false;
                }

                // One more check in case if _queue has been purged while in pause.
                if (Count > 0) {
                    if (Pop(out TObject item)) {
                        _receiver(item);
                    }
                }
            }
            _Cleanup();
        }

        private void _Cleanup()
        {
            lock (_receiverTaskLock) {

                _RaiseStateChangeEvent(Events.ProcessorTaskCanclled);
                _receiverTaskAutoEvent = null;
                _receiverTaskCts = null;
                _processorTaskCt = default;
                _receiverTask = null;
            }
        }

        private void _StartReceiverTask()
        {

            if (_receiverTask == null) {

                lock (_receiverTaskLock) {

                    _receiverTaskCts = new CancellationTokenSource();
                    _processorTaskCt = _receiverTaskCts.Token;
                    _receiverTask = new Task(_ReceiverWorker);
                    _receiverTask.Start();
                }
            }
            _RaiseStateChangeEvent(Events.ProcessorTaskStarted);
        }
    }
}
