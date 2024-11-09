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

namespace Grumpy.DAQFramework.Common
{ 
    public class BasicQueue<TObject> : QueueBase<TObject>, IDisposable
    {
        public enum Events
        {
            NA,
            Empty,
            Overflow,
            Full,
            BelowLowThreshould,
            AboveHighThreshould,
            ProcessorTaskStarted,
            ProcessorTaskPaused,
            ProcessorTaskResumed,
            Purged,
            ProcessorTaskCanclled,
        }

        public delegate void StateChangeEventHandler(object sender, BasicQueue<TObject>.Events e);
        public delegate void DataReadyEventHandler(object sender, TObject e);

        #region Constants

        public const int WorkerCancellationTimeoutMs = 2500;
        public const int DefaultMaxCapacity = 64;
        public const int DefalultLowThreshould = 16;
        public const int DefalultHighThreshould = 48;
        public const int DefalultNoThreshould = -1;
        public const string QueNamePreffix = "CPQ_";

        #endregion Constants

        #region Handlers
        public event StateChangeEventHandler? StateChangeEvent;
        public event DataReadyEventHandler? DataReadyEvent;
        #endregion 

        #region Private members
        private int _lostItemsCount;
        private bool _useAsyncEvents;
        #endregion Private members

        #region Constructors
        public BasicQueue(int maxDepth = DefaultQueueDepth, string? name = null,

            bool syncEvents = true) : base(maxDepth, name) {
            LowerThreshould = DefalultLowThreshould;
            UpperThreshould = DefalultHighThreshould;
            _useAsyncEvents = !syncEvents;
            _lostItemsCount = 0;
        }

        public BasicQueue(bool syncEvents) : this() {
            _useAsyncEvents = !syncEvents;
        }

        public BasicQueue(string name, bool syncEvents = false) :
            this(DefaultQueueDepth, name, syncEvents) { }
        #endregion Consructors

        #region Public properties
        public int NumberOfLostItems {
            get {
                int ret;
                Thread.MemoryBarrier();
                ret = _lostItemsCount;
                Thread.MemoryBarrier();
                return ret;
            }
            set {
                Thread.MemoryBarrier();
                _lostItemsCount = value;
                Thread.MemoryBarrier();
            }
        }

        public int LowerThreshould { get; set; }

        public int UpperThreshould { get; set; }
        #endregion Public properties

        #region Public methods
        public void ResetStats() {
            NumberOfLostItems = 0;
        }

        public virtual bool Push(TObject obj, bool force = false) {
            lock (_queueLock) {

                return _Push(obj, force);
            }
        }

        protected bool _Push(TObject obj, bool force = false) {
           
            if (_queue == null) {
                return false;
            }

            if (force) {
                if (_MakeRoom(1, out int itemsRemoved)) {
                    _queue.Enqueue(obj);
                    _lostItemsCount += itemsRemoved;
                    return true;
                }
                else { return false; }
            }
            else {
                try {
                    _Enqueue(obj);
                    return true;
                }
                catch {
                    return false;
                }
            }
        }

        public virtual bool Pop(out TObject item) => base.TryDequeue(out item!);

        public override bool Purge() {
            if (base.Purge()) {
                _RaiseStateChangeEvent(Events.Purged);
                return true;
            }
            return false;
        }
        #endregion Public methods

        #region Protected methods.
        protected virtual void _RaiseStateChangeEvent(Events evnt) {
            if (StateChangeEvent != null) {

                if (_useAsyncEvents) {
                    Task.Run(() => StateChangeEvent.Invoke(this, evnt));
                }
                else {
                    StateChangeEvent.Invoke(this, evnt);
                }
            }
        }

        protected virtual void _RaiseDataReadyEvent(TObject data) {
            if (DataReadyEvent != null) {

                if (_useAsyncEvents) {
                    Task.Run(() => DataReadyEvent.Invoke(this, data));
                }
                else {
                    DataReadyEvent.Invoke(this, data);
                }
            }
        }
        #endregion  Protected methods.
    }
}
