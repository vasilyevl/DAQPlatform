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

using System.Collections.Concurrent;

namespace Grumpy.SDAQFramework.Common
{
    /// <summary>
    /// Represents errors that occur during queue operations.
    /// </summary>
    public class QueueException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueException"/> class with a specified error description.
        /// </summary>
        /// <param name="description">The error description.</param>
        public QueueException(string description) :
            base(description)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueException"/> class with a specified error description and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="description">The error description.</param>
        /// <param name="e">The exception that is the cause of the current exception.</param>
        public QueueException(string description, Exception e) :
            base(description, e)
        { }
    }

    /// <summary>
    /// Represents errors that occur during enqueue operations in a queue.
    /// </summary>
    public class EnqueueException : QueueException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnqueueException"/> class with a specified error description.
        /// </summary>
        /// <param name="description">The error description.</param>
        public EnqueueException(string description) :
            base(description)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnqueueException"/> class with a specified error description and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="description">The error description.</param>
        /// <param name="e">The exception that is the cause of the current exception.</param>
        public EnqueueException(string description, Exception e) :
            base(description, e)
        {
#if DEBUG
            Console.WriteLine(description);
#endif
        }
    }

    public interface IThreadSafeQueueBase<TItem>
    {
        bool AtCapacity { get; }
        int Count { get; }
        List<string>? ErrorHistory { get; }
        ulong Id { get; }
        bool IsEmpty { get; }
        bool ItemPending { get; }
        string LastError { get; }
        int MaxDepth { get; set; }
        string Name { get; }
        int RoomLeft { get; }
        void Dispose();
        bool InsertAt(TItem item, int index);
        bool InsertInfront(TItem[] items);
        bool MakeRoom(int roomRequested, out int itemsRemoved);
        TItem[]? PeekAllAsArray();
        List<TItem>? PeekAllAsList();
        bool Purge();
    }

    /// <summary>
    /// Provides a thread-safe, bounded queue implementation with error tracking and utility methods.
    /// </summary>
    /// <typeparam name="TItem">The type of elements stored in the queue.</typeparam>
    public class ThreadSafeQueueBase<TItem> : IThreadSafeQueueBase<TItem>
    {
        /// <summary>
        /// Global counter for all queue instances.
        /// </summary>
        internal static ulong QueueCounter = 0;

        /// <summary>
        /// Default depth for error history.
        /// </summary>
        internal const int DefaultErrorHistoryDepth = 16;

        /// <summary>
        /// Default maximum queue depth.
        /// </summary>
        public const int DefaultQueueDepth = 64;

        /// <summary>
        /// The underlying concurrent queue.
        /// </summary>
        protected ConcurrentQueue<TItem>? _queue;

        /// <summary>
        /// Lock object for synchronizing queue operations.
        /// </summary>
        protected object _queueLock;

        private ErrorHistory _errorHistory;
        private int _maxDepth;
        private readonly string? _name;
        private ulong _id;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeQueueBase{TItem}"/> class.
        /// </summary>
        /// <param name="maxDepth">The maximum number of items the queue can hold.</param>
        /// <param name="name">The name of the queue instance.</param>
        public ThreadSafeQueueBase(int maxDepth = DefaultQueueDepth, string? name = null)
        {
            QueueCounter++;
            _id = QueueCounter;
            _maxDepth = maxDepth;
            _queueLock = new object();

            _queue = new ConcurrentQueue<TItem>();
            _errorHistory = Common.ErrorHistory.Create(maxCapacity: DefaultErrorHistoryDepth);
            _name = name != null ? name : $"Queue_{QueueCounter}";
        }

        /// <summary>
        /// Gets the name of the queue.
        /// </summary>
        public string Name => (string) (_name?.Clone() ?? string.Empty);


        /// <summary>
        /// Unique Queue ID
        /// </summary>
        public ulong Id => _id;

        /// <summary>
        /// Gets or sets the maximum number of items the queue can hold.
        /// </summary>
        public int MaxDepth
        {
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

        /// <summary>
        /// Gets a value indicating whether there are items pending in the queue.
        /// </summary>
        public bool ItemPending => !(_queue?.IsEmpty ?? true);

        /// <summary>
        /// Gets the current number of items in the queue.
        /// </summary>
        public int Count => _queue?.Count ?? 0;

        /// <summary>
        /// Gets the number of additional items that can be enqueued before reaching capacity.
        /// </summary>
        public int RoomLeft
        {
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

        /// <summary>
        /// Gets a value indicating whether the queue is empty.
        /// </summary>
        public bool IsEmpty => (_queue?.IsEmpty ?? true);

        /// <summary>
        /// Gets a value indicating whether the queue is at capacity.
        /// </summary>
        public bool AtCapacity => RoomLeft <= 0;

        /// <summary>
        /// Gets or sets the last error message encountered by the queue.
        /// </summary>
        public string LastError
        {
            get {
                if (_errorHistory != null) {
                    if (_errorHistory.Peek(out LogRecord r)) {
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

        /// <summary>
        /// Gets the history of error messages encountered by the queue.
        /// </summary>
        public List<string>? ErrorHistory
        {
            get {
                if (_errorHistory != null) {
                    return _errorHistory
                         .PeekAllAsArray(lastFirst: false)
                         .Select((s) => s.Details).ToList();
                }
                return new List<string>();
            }
        }

        /// <summary>
        /// Attempts to peek at the item at the front of the queue without removing it.
        /// </summary>
        /// <param name="item">The item at the front of the queue, if available.</param>
        /// <returns>True if an item was successfully peeked; otherwise, false.</returns>
       protected bool TryPeek(out TItem? item)
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

        /// <summary>
        /// Attempts to dequeue an item from the front of the queue.
        /// </summary>
        /// <param name="item">The dequeued item, if available.</param>
        /// <returns>True if an item was successfully dequeued; otherwise, false.</returns>
        protected bool TryDequeue(out TItem? item)
        {
            lock (_queueLock) {

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
        }

        /// <summary>
        /// Attempts to enqueue an item to the queue.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        /// <returns>True if the item was successfully enqueued; otherwise, false.</returns>
        protected virtual bool TryEnqueue(TItem item)
        {
            lock (_queueLock) {
                return _Enqueue(item);
            }
        }

        /// <summary>
        /// Enqueues an item to the queue if there is room.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        /// <returns>True if the item was successfully enqueued; otherwise, false.</returns>
        protected bool _Enqueue(TItem item)
        {
            if ((_queue != null) && ((_maxDepth < 0) || (_queue.Count < _maxDepth))) {
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

        /// <summary>
        /// Attempts to insert an item at the specified index in the queue.
        /// </summary>
        /// <param name="item">The item to insert.</param>
        /// <param name="index">The zero-based index at which to insert the item.</param>
        /// <returns>True if the item was successfully inserted; otherwise, false.</returns>
        public virtual bool InsertAt(TItem item, int index)
        {
            lock (_queueLock) {
                return _InsertAt(item, index);
            }
        }

        private bool _InsertAt(TItem item, int index)
        {
            if (_queue == null) {
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

        /// <summary>
        /// Inserts an item at the front of the queue.
        /// </summary>
        /// <param name="item">The item to insert.</param>
        /// <returns>True if the item was successfully inserted; otherwise, false.</returns>
        protected virtual bool InsertFirst(TItem item)
        {
            lock (_queueLock) {
                return _InsertAt(item, 0);
            }
        }

        /// <summary>
        /// Inserts an array of items at the front of the queue.
        /// </summary>
        /// <param name="items">The items to insert.</param>
        /// <returns>True if the items were successfully inserted; otherwise, false.</returns>
        public virtual bool InsertInfront(TItem[] items)
        {
            lock (_queueLock) {
                return _InsertInfront(items);
            }
        }

        /// <summary>
        /// Inserts an array of items at the front of the queue (internal implementation).
        /// </summary>
        /// <param name="items">The items to insert.</param>
        /// <returns>True if the items were successfully inserted; otherwise, false.</returns>
        private bool _InsertInfront(TItem[] items)
        {
            if (_queue == null) {
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

        /// <summary>
        /// Removes all items from the queue.
        /// </summary>
        /// <returns>True if the queue was successfully purged; otherwise, false.</returns>
        public virtual bool Purge()
        {
            lock (_queueLock) {
                return _Purge();
            }
        }

        /// <summary>
        /// Removes all items from the queue (internal implementation).
        /// </summary>
        /// <returns>True if the queue was successfully purged; otherwise, false.</returns>
        private bool _Purge()
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

        /// <summary>
        /// Attempts to make room in the queue by removing items if necessary.
        /// </summary>
        /// <param name="roomRequested">The number of slots to free.</param>
        /// <param name="itemsRemoved">The number of items actually removed.</param>
        /// <returns>True if enough room was made; otherwise, false.</returns>
        public bool MakeRoom(int roomRequested, out int itemsRemoved)
        {
            lock (_queueLock) {
                return _MakeRoom(roomRequested, out itemsRemoved);
            }
        }

        /// <summary>
        /// Attempts to make room in the queue by removing items if necessary (internal implementation).
        /// </summary>
        /// <param name="roomRequested">The number of slots to free.</param>
        /// <param name="itemsRemoved">The number of items actually removed.</param>
        /// <returns>True if enough room was made; otherwise, false.</returns>
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
                while (_queue.Count > 0 && _maxDepth - _queue.Count < roomRequested) {
                    if (_queue.TryDequeue(out TItem? item)) {
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

        /// <summary>
        /// Returns all items in the queue as an array, with the most recently enqueued item last.
        /// </summary>
        /// <returns>An array of all items in the queue, or null if the queue is empty or unavailable.</returns>
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

        /// <summary>
        /// Returns all items in the queue as a list, with the most recently enqueued item last.
        /// </summary>
        /// <returns>A list of all items in the queue, or null if the queue is empty or unavailable.</returns>
        public List<TItem>? PeekAllAsList()
        {
            TItem[]? array = PeekAllAsArray();

            if (array != null) {
                return array.ToList();
            }
            return null;
        }

        /// <summary>
        /// Releases all resources used by the queue and purges its contents.
        /// </summary>
        public virtual void Dispose()
        {
            _Purge();
            _queue = null;
        }
    }
}