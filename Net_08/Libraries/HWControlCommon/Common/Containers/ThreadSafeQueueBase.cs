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

using System.Collections.Concurrent;

namespace Grumpy.DAQFramework.Common
{
    public class QueueBase<TItem>
    {

        internal static ulong QueueCounter = 0;
        internal const int DefaultErrorHistoryDepth = 16;

        public const int DefaultQueueDepth = 64;

        protected ConcurrentQueue<TItem>? _queue;

        protected object _queueLock;
        
        private ErrorHistory _errorHistory;
        private int _maxDepth;
        private readonly string? _name;
 

        public QueueBase(int maxDepth = DefaultQueueDepth, string? name = null) 
        {           
            QueueCounter++;

            _maxDepth = maxDepth;
            _queueLock = new object();

            _queue = new ConcurrentQueue<TItem>();
            _errorHistory = Common.ErrorHistory.Create(maxCapacity: DefaultErrorHistoryDepth);
            _name = name != null ? name : $"Queue_{QueueCounter}";
        }

        public string Name => (string)(_name?.Clone() ?? string.Empty);

        public int MaxDepth {
            get {
                lock (_queueLock) {
                    return _maxDepth;
                }
            }

            set {
                lock (_queueLock) {
                    _maxDepth = value;
                }
            }
        }

        public bool ItemPending => !(_queue?.IsEmpty ?? true);

        public int Count => _queue?.Count ?? 0;
                
        public int RoomLeft {
            get {
                lock (_queueLock) {

                    return _RoomLeft();
                }
            }
        }

        private int _RoomLeft()
        {

            return _maxDepth > 0 ?
                Math.Max(0, _maxDepth - (_queue?.Count ?? 0)) :
                int.MaxValue;

        }

        public bool IsEmpty => (_queue?.IsEmpty ?? true);

        public bool AtCapacity => RoomLeft <= 0;


        public string LastError {

            get {

                if (_errorHistory != null) {

                    if (_errorHistory.Peek(out LogRecord? r)) {

                        return r?.Details ?? string.Empty;
                    }
                }

                return string.Empty;
            }
        
            protected set {

                if (!string.IsNullOrEmpty(value)) {
                
                    _errorHistory.Push(
                        new LogRecord(LogLevel.Error, "", value, -1),
                        force: true);
                }
            }
        }

        public List<string>? ErrorHistory {

            get {

                if (_errorHistory != null) {

                    return _errorHistory
                         .PeekAllAsArray(reverseOrder: false)
                         .Select((s) => s.Details).ToList();
                }

                return new List<string>();
            }
        }

        public bool TryPeek(out TItem? item)
        {
            item = default(TItem);

            try {

                return _queue?.TryPeek(out item) ?? false;
            }
            catch (Exception ex) {

                item = default;
                LastError = ($"Failed to deque item from" +
                    $" fifo {Name}. Exception: {ex.Message}");
                return false;
            }
        }

        public bool TryDequeue(out TItem? item)
        {
            try {

                item = default(TItem);
                return _queue?.TryDequeue(out item) ?? false;
            }
            catch (Exception ex) {

                LastError = $"Failed to dequeue item from " +
                    $"queue {Name}. Exception: {ex.Message}";
                item = default;
                return false;
            }
        }

        public virtual bool TryEnqueue(TItem item)
        {
            lock (_queueLock) {

                return _Enqueue(item);
            }
        }

        protected bool _Enqueue(TItem item)
        {
            
            if ( (_queue != null) && ((_maxDepth < 0) || (_queue.Count < _maxDepth))) {
                try {
                    _queue.Enqueue(item);
                    return true;
                }
                catch (Exception e) {
                    LastError = $"Exception while adding " +
                        $"item {item?.GetType().Name ?? "null"} to queue {_name}. " +
                        $"Exception: {e.Message}";
                    return false;
                }
            }
            else {
                LastError = $"Exception while adding " +
                    $"item {item?.GetType().Name ?? "null"} to queue {_name}. " +
                    $"Queue at capacity {_maxDepth}";

                return false;
            }
        }

        public virtual bool InsertAt(TItem item, int index)
        {
            lock (_queueLock) {

                return _InsertAt(item, index);
            }
        }

        private bool _InsertAt(TItem item, int index)
        {
            if(_queue == null) {
                LastError = $"Queue {Name} is null. " +
                    $"Can't insert item at index {index}.";
                return false;
            }


            if (_queue.Count >= _maxDepth) {

                LastError = $"Can't insert. No room left. ";
                return false;
            }
         
            index = Math.Max(0, index);

            try {

                if (index >= _queue.Count) {
                    index = _queue.Count;
                    
                    _queue.Enqueue(item);
                    return true;
                }
                else {
                    TItem[] arr = _queue.ToArray();
                    Array.Resize(ref arr, arr.Length + 1);
                    Array.Copy(arr, index, arr, index + 1, arr.Length - index - 1);
                    arr[index] = item;
                    _queue = new ConcurrentQueue<TItem>(arr);
                    return true;
                }
            }
            catch (Exception ex) {
                LastError = $"Failed to insert item at index{index} " +
                    $"in queue {Name}. Exception: {ex.Message}";
                return false;
            }
        }

        public virtual bool InsertFirst(TItem item)
        {
            lock (_queueLock) {

                return _InsertAt(item, 0);
            }
        }


        public virtual bool InsertInfront(TItem[] items)
        {
            lock (_queueLock) {
                return _InsertInfront(items);
            }
        }

        protected bool _InsertInfront(TItem[] items)
        {
            if(_queue == null) {
                LastError = $"Queue {Name} is null. " +
                    $"Can't insert items infront.";
                return false;
            }

            if (items.Length > (_maxDepth - _queue.Count)) {

                LastError = $"Failed to insert items infront " +
                    $"in queue {Name}. Number of items exceeds " +
                    $"max queue depth.";
                return false;
            }

            try {
                Queue<TItem>? tmp = null;
                if (_queue.Count > 0) {

                    tmp = new Queue<TItem>();

                    while (_queue.Count > 0) {

                        if (_queue.TryDequeue(out var st)) {
                            tmp.Enqueue(st);
                        }
                    }
                }

                foreach (var item in items) {
                    _queue.Enqueue(item);
                }

                if (tmp != null) {

                    while (tmp.Count > 0) {
                        _queue.Enqueue(tmp.Dequeue());
                    }
                }
                return true;
            }
            catch (Exception ex) {

                LastError = $"Failed to insert items infront " +
                    $"in queue {Name}. Exception: {ex.Message}.";
                return false;
            }            
        }
        
        public virtual bool Purge()
        {
            lock (_queueLock) {
                return _Purge();
            }
        }

        protected virtual bool _Purge()
        {
            try {
                if (_queue == null) {
                    return true;
                }

                while (_queue.Count > 0) {

                    _queue.TryDequeue(out var it);
                }

                return _queue.IsEmpty;
            }
            catch (Exception ex) {
                LastError = $"Failed to purge queue {Name}. " +
                    $"Exception: {ex.Message}.";
                return false;
            }
        }

        public bool MakeRoom(int roomRequested, out int itemsRemoved)
        {
            lock (_queueLock) {

                return _MakeRoom(roomRequested, out itemsRemoved);
            }
        }

        protected bool _MakeRoom(int roomRequested, out int itemsRemoved)
        { 
                itemsRemoved = 0;

            if (_queue == null) {
                LastError = $"Queue {Name} is null. " +
                    $"Can't free room for {roomRequested} items.";
                return false;
            }

            if (_maxDepth - _queue.Count >= roomRequested) {
                    return true;
                }

                if (_queue.Count > 0) {

                    while( _queue.Count > 0 && _maxDepth - _queue.Count < roomRequested) { 
                    
                        if(_queue.TryDequeue(out TItem? item)) {
                            itemsRemoved++;
                        }
                    }
                }

            if (roomRequested <= (_maxDepth - _queue.Count)) { 
                return true;
            } 
            else {
                LastError = $"Queue {Name}. Failed to free enough room to " +
                        $"accomodate {roomRequested} items." +
                        $" {((roomRequested > _maxDepth) ? $"Requested count exceeds max depth {_maxDepth}." : "")}";
                return false;
            }          
        }

        public TItem[]? PeekAllAsArray()
        {
            lock (_queueLock) {

                if (_queue == null) {
                    LastError = $"Queue {Name} is null. " +
                        $"Can't peek items as array.";
                    return null;
                }

                if (_queue.Count > 0) {
                    try {
                        TItem[] array = _queue.ToArray();

                        if (array != null) {
                            Array.Reverse(array);
                        }

                        return array!;
                    }
                    catch (Exception e) {

                        LastError = $"Queue {Name}. Failed to peek items as array. " +
                            $"Exception: {e.Message}.";
                        return null;
                    }
                }
                else {
                    LastError = $"Queue {Name}. No items to peek. Array is Empty.";
                    return null;
                }
            }
        }

        public List<TItem>? PeekAllAsList()
        {
            TItem[]? array = PeekAllAsArray();

            if ( array != null) {
                return array.ToList();
            }
            return null;
        }

        public virtual void Dispose()
        {
            _Purge();
            _queue = null;
        }
    }

    public class QueueException : Exception
    {
        public QueueException(string description) :
            base(description)
        { }
        public QueueException(string description, Exception e) :
            base(description, e)
        { }
    }
    public class EnqueueException : QueueException
    {
        public EnqueueException(string description) :
            base(description)
        { }

        public EnqueueException(string description, Exception e) :
            base(description, e)
        {
#if DEBUG
            Console.WriteLine(description);
#endif
        }

    }



}
