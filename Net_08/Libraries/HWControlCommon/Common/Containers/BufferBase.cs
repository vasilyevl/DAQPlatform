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

using System.Runtime.CompilerServices;

namespace Grumpy.SDAQFramework.Common
{
    /// <summary>
    /// Provides a thread-safe, capacity-limited buffer collection with threshold notifications and flexible item management.
    /// </summary>
    /// <typeparam name="Titem">The type of elements stored in the buffer.</typeparam>
    /// <remarks>
    /// <para>
    /// <b>BufferBase</b> is a generic base class for implementing buffer-like collections. It supports thread-safe operations,
    /// configurable maximum capacity, and threshold-based events for monitoring buffer state changes.
    /// </para>
    /// <para>
    /// The buffer uses a <see cref="LinkedList{Titem}"/> internally and provides methods for adding, removing, peeking,
    /// and making room for items. It also exposes events for when the buffer becomes empty, reaches capacity, or crosses
    /// user-defined thresholds.
    /// </para>
    /// <para>
    /// All public and protected members that access the buffer are thread-safe via a <see cref="ReaderWriterLockSlim"/>.
    /// </para>
    /// </remarks>
    public class BufferBase<Titem> : DisposableBase, IBufferBase<Titem>
    {
        private const int _MinCapacity = 2;
        private const int _DefaultSize = 32;
        private const string _DefaultNamePreffix = "Buffer_";
        private const int _NoTheshold = -1;

        private static int _objectCounter = 0;

        private readonly int _id = 0;
        private readonly string _name;

        private readonly ReaderWriterLockSlim _collectionLock;

        private LinkedList<Titem> _collection;

        private int _maxCapacity;
        private int _lowerThreshold;
        private int _upperThreshold;

        public event EventHandler? HasBecomeEmpty;
        public event EventHandler? HasReachedCapacity;
        public event EventHandler? AtLowerThreshold;
        public event EventHandler? AtUpperThreshold;
        public event EventHandler<int>? ItemAdded;
        public event EventHandler<int>? ItemsDiscarded;


        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BufferBase{Titem}"/> class with default capacity and thresholds.
        /// </summary>
        public BufferBase():base() {

            _id = Interlocked.Increment(ref _objectCounter);
            _name = $"{_DefaultNamePreffix}{_id}";
            _maxCapacity = _DefaultSize;
            _lowerThreshold = _NoTheshold;
            _upperThreshold = _NoTheshold;
            _collectionLock = new ReaderWriterLockSlim();
            _collection = new LinkedList<Titem>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferBase{Titem}"/> class with the specified maximum capacity and optional name.
        /// </summary>
        /// <param name="maxCapacity">The maximum number of items the buffer can hold.</param>
        /// <param name="name">The optional name of the buffer.</param>
        public BufferBase(int maxCapacity, string? name = null): this() {
                      
            _maxCapacity = System.Math.Max(maxCapacity, _MinCapacity);
            _name = string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name)
                ? _name : name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferBase{Titem}"/> class with the specified capacity, initial value, thresholds, and optional name.
        /// </summary>
        /// <param name="maxCapacity">The maximum number of items the buffer can hold.</param>
        /// <param name="initialValue">The initial value to add to the buffer.</param>
        /// <param name="lowerThreshould">The lower threshold for notifications.</param>
        /// <param name="upperThreshould">The upper threshold for notifications.</param>
        /// <param name="name">The optional name of the buffer.</param>
        public BufferBase(int maxCapacity, Titem initialValue, 
            int lowerThreshould,  int upperThreshould, 
            string? name = null) : this(maxCapacity, name) {

            try {
                _collection.AddLast(initialValue);
                _lowerThreshold = System.Math.Min(lowerThreshould, maxCapacity);
                _upperThreshold = System.Math.Min(System.Math.Max(lowerThreshould, upperThreshould), maxCapacity);
            }
            catch (Exception ex) {
                
                throw new InvalidOperationException(
                    $"Error adding initial value to " +
                    $"Collection \"{name}\": {ex.Message}");
            }
        }

        #endregion Constructors


        #region Helpers
        /// <summary>
        /// Checks if the buffer is empty.
        /// Private inline helper method. Not thread-safe.  
        /// </summary>
        /// <returns>True if the buffer is empty; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool _BufferIsEmpty()=> (_collection?.Count ?? 0) == 0 ;

        /// <summary>
        /// Checks if the buffer is at capacity.
        /// Private inline helper method. Not thread-safe.
        /// </summary>
        /// <param name="error">Output error message if at capacity.</param>
        /// <returns>True if the buffer is at capacity; otherwise, false.</returns>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool _AtCapacity(out string error) {

            if (_collection.Count >= _maxCapacity) {

                error = "Collection is at capacity.";
                return true;
            }
            error = string.Empty;
            return false;
        }


        /// <summary>
        /// Checks if the buffer is null or empty and sets an error message if so.
        /// Private inline helper method. Not thread-safe.
        /// </summary>
        /// <typeparam name="T">The type of the value to output.</typeparam>
        /// <param name="error">Output error message if buffer is empty.</param>
        /// <param name="value">Output value (default if empty).</param>
        /// <returns>True if the buffer is null or empty; otherwise, false.</returns>
        private bool _CheckIfBufferIsEmpty<T>( out string error, out T value) {
            
            value = default!;

            if (_BufferIsEmpty()) {
                error = "Buffer is empty.";
                return true;
            }

            error = string.Empty;
            return false;
        }

        /// <summary>
        /// Attempts to get the node at the specified index in the buffer. 
        /// Protected helper method. Not thread-safe.
        /// </summary>
        /// <param name="index">The zero-based index of the node.</param>
        /// <param name="node">The node at the specified index, if found.</param>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <returns>True if the node was found; otherwise, false.</returns>
        protected bool TryGetNodeAt(int index, out LinkedListNode<Titem>? node, out string error) {
            node = _collection.First;
            for (int i = 0; i < index && node != null; i++)
                node = node.Next;
            if (node == null) {
                error = "Index out of range.";
                return false;
            }
            error = string.Empty;
            return true;
        }


        /// <summary>
        /// Raises buffer-related events asynchronously based on the buffer state.
        /// Protected inline helper method.  Not thread-safe.
        /// </summary>
        /// <param name="itemAdded">Indicates if an item was added.</param>
        /// <param name="capacityWillBeReached">Indicates if capacity will be reached.</param>
        /// <param name="upperThresholdWillBeReached">Indicates if the upper threshold will be reached.</param>
        /// <param name="itemsAddedCount">The number of items added (default is 1).</param>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void RaiseAddItemEvents(
            bool itemAdded,
            bool capacityWillBeReached,
            bool upperThresholdWillBeReached,
            int itemsAddedCount = 1) {

            if ((itemAdded && ItemAdded != null)
                || (capacityWillBeReached && HasReachedCapacity != null)
                || (upperThresholdWillBeReached && AtUpperThreshold != null)) {

                Task.Factory.StartNew(() => {
                    if (itemAdded)
                        ItemAdded?.Invoke(this, itemsAddedCount);

                    if (capacityWillBeReached)
                        HasReachedCapacity?.Invoke(this, EventArgs.Empty);

                    if (upperThresholdWillBeReached)
                        AtUpperThreshold?.Invoke(this, EventArgs.Empty);
                });
            }
        }



        /// <summary>
        /// Method to raise events after a successful pop operation.
        /// Protected helper method. Not thread-safe.
        /// </summary>
        /// <param name="popped">True if an item was popped.</param>
        /// <param name="aboutToBecomeEmpty">True if the buffer is about to become empty.</param>
        /// <param name="atLowerThreshold">True if the buffer is at the lower threshold.</param>
        protected void RaisePopItemEvents(bool popped, bool aboutToBecomeEmpty, bool atLowerThreshold) {
            if (popped) {

                Task.Factory.StartNew(() => {
                    if (aboutToBecomeEmpty) {

                        HasBecomeEmpty?.Invoke(this, EventArgs.Empty);
                    }

                    if (atLowerThreshold) {

                        AtLowerThreshold?.Invoke(this, EventArgs.Empty);
                    }
                });

            }
        }




        #endregion Helpers

        #region Public Properties
        /// <summary>
        /// Gets or sets the lower threshold for buffer notifications in a thread-safe manner.
        /// </summary>
        public int LowerThreshold {
            get {
                _collectionLock.EnterReadLock();
                try {
                    return _lowerThreshold;
                }
                finally {
                    _collectionLock.ExitReadLock();
                }
            }
            set {
                _collectionLock.EnterWriteLock();
                try {
                    _lowerThreshold = value;
                }
                finally {
                    _collectionLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the upper threshold for buffer notifications in a thread-safe manner.
        /// </summary>
        public int UpperThreshold {
            get {
                _collectionLock.EnterReadLock();
                try {
                    return _upperThreshold;
                }
                finally {
                    _collectionLock.ExitReadLock();
                }
            }
            set {
                _collectionLock.EnterWriteLock();
                try {
                    _upperThreshold = value;
                }
                finally {
                    _collectionLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets the unique identifier for this buffer instance.
        /// </summary>
        public int ID => _id;

        /// <summary>
        /// Gets the name of the buffer.
        /// </summary>
        public string Name => (string)_name.Clone();

        /// <summary>
        /// Gets or sets the maximum capacity of the buffer in a thread-safe manner.
        /// </summary>
        public int MaxCapacity {
            get {
                _collectionLock.EnterReadLock();
                try {
                    return _maxCapacity;
                }
                finally {
                    _collectionLock.ExitReadLock();
                }
            }
            set {
                _collectionLock.EnterWriteLock();
                try {
                    _maxCapacity = System.Math.Max(value, _MinCapacity);
                }
                finally {
                    _collectionLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets the available capacity in the buffer in a thread-safe manner.
        /// </summary>
        public int AvailableCapacity {
            get {
                _collectionLock.EnterReadLock();
                try {
                    return System.Math.Max(0,_maxCapacity - _collection.Count);
                }
                finally {
                    _collectionLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the number of items in the buffer in a thread-safe manner.
        /// </summary>
        public int Count {
            get {
                _collectionLock.EnterReadLock();
                try {
                    return _BufferIsEmpty() ? 0 : _collection.Count;
                }
                finally {
                    _collectionLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Checks whether the buffer is empty in a thread-safe manner.
        /// True if empty, otherwise, false.
        /// </summary>
        public bool IsEmpty {
            get {
                _collectionLock.EnterReadLock();
                try {
                    return _BufferIsEmpty();
                }
                finally {
                    _collectionLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Checks whether the buffer is at capacity in a thread-safe manner.
        /// True if at capacity, otherwise, false
        /// </summary>
        public bool IsAtCapacity {
            get {
                _collectionLock.EnterReadLock();
                try {

                    return _BufferIsEmpty() ? false: _collection.Count >= _maxCapacity;
                }
                finally {
                    _collectionLock.ExitReadLock();
                }
            }
        }


        /// <summary>
        /// Checks whether the buffer has room for at least one more item in a thread-safe manner.
        /// True if there is room, otherwise, false.
        /// </summary>
        public bool HasRoom {
            get {
                _collectionLock.EnterReadLock();
                try {
                    return _BufferIsEmpty() ? true : _collection.Count < _maxCapacity;
                }
                finally {
                    _collectionLock.ExitReadLock();
                }
            }
        }

        #endregion Public Properties


        #region Public Methods

        /// <summary>
        /// Attempts to add an item to the buffer. Thread-safe.
        /// </summary>
        /// <param name="value">The item to add.</param>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <returns>True if the item was added; otherwise, false.</returns>
        public bool TryAdd(Titem value, out string error) {

            _collectionLock.EnterWriteLock();

            bool capacityWillBeReached = false;
            bool upperThresholdWillBeReached = false;
            bool itemAdded = false;
            int count = -1;

            try {
                // Use the private _AtCapacity() method for capacity check
                if (_AtCapacity(out error))
                    return false;

                capacityWillBeReached = _collection.Count == _maxCapacity - 1;
                upperThresholdWillBeReached =
                    (_collection.Count == _upperThreshold - 1)
                    && (_upperThreshold >= 1) && (_upperThreshold != _NoTheshold);
                _collection.AddLast(value);
                error = string.Empty;
                count = _collection.Count;
                itemAdded = true;           
                return true;
            }
            catch (Exception ex) {
                error = $"Error adding value to collection: {ex.Message}";
                return false;
            }
            finally {
                
                _collectionLock.ExitWriteLock();
                RaiseAddItemEvents(itemAdded, capacityWillBeReached, upperThresholdWillBeReached, 1);

            }
        }

        /// <summary>
        /// Attempts to add multiple items to the buffer. Thread-safe.
        /// </summary>
        /// <param name="items">The items to add.</param>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <returns>True if the items were added; otherwise, false.</returns>
        public bool TryAddItems(Titem[] items, out int itemsAdded, out string error) {
            _collectionLock.EnterWriteLock();

            itemsAdded = 0;
            bool capacityWillBeReached = false;
            bool upperThresholdWillBeReached = false;
            bool itemAdded = false;

            try {
                // Use the private _AtCapacity() method for capacity check
                if (_AtCapacity(out error))
                    return false;

                if (items == null || items.Length == 0) {
                    return true; // Nothing to add, return true
                }

                for (int i = 0; i < items.Length; i++) {

                    if (_collection.Count >= _maxCapacity) {
                        error = $"Collection is at capacity. " +
                            $"{itemsAdded} items out of " +
                            $"{items.Count()} added.";
                        return false;
                    }
                    capacityWillBeReached = (_maxCapacity - _collection.Count) == 1 || capacityWillBeReached;
                    upperThresholdWillBeReached = ((_collection.Count == _upperThreshold - 1) && (_upperThreshold >= 1) && (_upperThreshold != _NoTheshold)) || upperThresholdWillBeReached;
                    _collection.AddLast(items[i]);
                    itemsAdded = i + 1;
                }
                itemAdded = true;
                return true;
            }
            catch (Exception ex) {
                error = $"Error adding value to collection: {ex.Message}";
                return false;
            }
            finally {
                
                _collectionLock.ExitWriteLock();
                RaiseAddItemEvents(itemAdded, capacityWillBeReached, upperThresholdWillBeReached, itemsAdded);
            }
        }

        /// <summary>
        /// Inserts the specified item at the front of the buffer. Thread-safe.
        /// </summary>
        /// <param name="value">The item to insert at the front of the collection.</param>
        /// <param name="error">Output parameter that contains an error message if the operation fails; otherwise, an empty string.</param>
        /// <returns>True if the item was successfully inserted; otherwise, false.</returns>
        public bool TryInsertFirst(Titem value, out string error) {
            _collectionLock.EnterWriteLock();

            bool capacityWillBeReached = false;
            bool upperThresholdWillBeReached = false;
            bool itemAdded = false;

            try {
                // Use the private _AtCapacity() method for capacity check
                if (_AtCapacity(out error))
                    return false;

                capacityWillBeReached = _collection.Count == _maxCapacity - 1;
                upperThresholdWillBeReached =
                    (_collection.Count == _upperThreshold - 1) && (_upperThreshold >= 1) && (_upperThreshold != _NoTheshold);

                _collection.AddFirst(value);

                error = string.Empty;
                itemAdded = true;
               
                return true;
            }
            catch (Exception ex) {
                error = $"Error inserting value at the front of the collection: {ex.Message}";
                return false;
            }
            finally {
               
                _collectionLock.ExitWriteLock();
                RaiseAddItemEvents(itemAdded, capacityWillBeReached, upperThresholdWillBeReached, 1);
            }
        }

        /// <summary>
        /// Attempts to insert an item at the specified index in the buffer. Thread-safe.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the item.</param>
        /// <param name="value">The item to insert.</param>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <returns>True if the item was inserted; otherwise, false.</returns>
        public bool TryInsertAt(int index, Titem value, out string error) {


            bool capacityReached = false;
            bool upperThresholReached = false;
            bool itemAdded = false; 
            _collectionLock.EnterWriteLock();

            if (_AtCapacity(out error)) {
                return false;
            }

            if (index < 0) {
                error = $"Index can't be negative {index}";
                return false;
            }

            index = System.Math.Min(index, _collection.Count);

            try {

                if (index <= 0) {
                    _collection.AddFirst(value);
                    error = string.Empty;
                }

                if (TryGetNodeAt(index - 1, out LinkedListNode<Titem>? node, out error)) {
                    _collection.AddAfter(node!, value);
                }
                else {
                    error = $"Index {index} is out of range for the collection.";
                    return false;
                }

                itemAdded = true;
                capacityReached = _collection.Count == _maxCapacity;
                upperThresholReached =
                    (_collection.Count == _upperThreshold) && (_upperThreshold >= 1) && (_upperThreshold != _NoTheshold);
                return true;
            }
            catch (Exception ex) {
                error = $"Error inserting element at index {index}: {ex.Message}";
                return false;
            }
            finally {
                _collectionLock.ExitWriteLock();
                RaiseAddItemEvents(itemAdded, capacityReached, upperThresholReached);
            }
        }

        /// <summary>
        /// Attempts to clear all items from the buffer. Thread-safe.
        /// </summary>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <returns>True if the buffer was cleared; otherwise, false.</returns>
        public bool TryClear(out string error) {

            bool cleared = false;
            _collectionLock.EnterWriteLock();
            
            try {
            
                _collection.Clear();
                error = string.Empty;
                cleared = true;
                return true;
            }
            catch (Exception ex) {
                
                error = $"Error clearing collection: {ex.Message}";
                return false;
            }
            finally {
                
                _collectionLock.ExitWriteLock();
                
                if (cleared) {
                
                    HasBecomeEmpty?.Invoke(this, EventArgs.Empty);
                }
            }
        }


        /// <summary>
        /// Returns all items in the buffer as an array. Does not remove items from the buffer. Thread-safe.
        /// </summary>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <param name="recentFirst">If true, the most recently added items appear first.</param>
        /// <returns>An array of items in the buffer.</returns>
        public Titem[] PeekAllAsArray(out string error, 
            bool recentFirst = false) {
            
            _collectionLock.EnterReadLock();
            
            try {
            
                var array = _collection.ToArray();
                if (recentFirst) {
                
                    Array.Reverse(array);
                }

                error = string.Empty;
                return array;
            }
            catch (Exception ex) {
                
                error = $"Error peeking all elements as array: {ex.Message}";
                return Array.Empty<Titem>();
            }
            finally {
                
                _collectionLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns all items in the buffer as a list. Does not remove items from the buffer. Thread-safe.
        /// </summary>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <param name="recentFirst">If true, the most recently added items appear first.</param>
        /// <returns>A list of items in the buffer.</returns>
        public List<Titem> PeekAllAsList(out string error, 
            bool recentFirst = false) {
            
            _collectionLock.EnterReadLock();
            
            try {
            
                var list = _collection.ToList();
                
                if (recentFirst) {
                
                    list.Reverse();
                }

                error = string.Empty;
                return list;
            }
            catch (Exception ex) {
                
                error = $"Error peeking all elements as list: {ex.Message}";
                return new List<Titem>();
            }
            finally {
                
                _collectionLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Attempts to make room for the specified number of items by removing items from the buffer. Thread-safe. 
        /// </summary>
        /// <param name="requiredCapacity">The number of items to make room for.</param>
        /// <param name="removedItemsCount">The number of items actually removed.</param>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <param name="removeFromFirst">If true, remove items from the front; otherwise, from the back.</param>
        /// <returns>True if enough room was made or already available; otherwise, false.</returns>
        public bool TryMakeRoom(int requiredCapacity, 
            out int removedItemsCount, 
            out string error,
            bool removeFromFirst = true) {
            
            _collectionLock.EnterWriteLock();
            error = string.Empty;
            removedItemsCount = 0;
            
            try {

                if (requiredCapacity <= _maxCapacity - _collection.Count) {
                    return true;
                }

                if (requiredCapacity > _maxCapacity) {
                    error = "Requested room exceeds maximum capacity.";
                    return false;
                }

                while (_collection.Count > (_maxCapacity - requiredCapacity)) {

                    if (_collection.Count == 0)
                        break;

                    if (removeFromFirst)
                        _collection.RemoveFirst();
                    else
                        _collection.RemoveLast();

                    removedItemsCount++;
                }

                return true;
            }
            catch (Exception ex) {
                error = $"Error while making room: {ex.Message}";
                return false;
            }
            finally {
                _collectionLock.ExitWriteLock();
                if (removedItemsCount > 0) {
                    ItemsDiscarded?.BeginInvoke(this, removedItemsCount, null, null);
                }
            }
        }

        /// <summary>
        /// Releases managed resources used by the buffer. Thread-safe.
        /// Supresses exceptions.
        /// </summary>
        protected override void DisposeManagedResources() {
            _collectionLock.EnterWriteLock();
            try {
                _collection.Clear();
            }
            finally {
                _collection = null!;
                _collectionLock.ExitWriteLock();
                _collectionLock.Dispose();
            }
        }

        /// <summary>
        /// Attempts to peek at the first item in the buffer without removing it. Thread-safe.
        /// </summary>
        /// <param name="value">The first item, if available.</param>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <returns>True if an item was available; otherwise, false.</returns>
        protected bool TryPeekFirst(out Titem? value, out string error) {
            _collectionLock.EnterReadLock();
            value = default;

            try {

                if (_CheckIfBufferIsEmpty(out error, out value)) {
                    return false;
                }

                value = _collection!.First!.Value!;
                error = string.Empty;
                return true;
            }
            catch (Exception ex) {
                error = $"Error peeking first element: {ex.Message}";
                value = default;
                return false;
            }
            finally {
                _collectionLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Attempts to remove and return the first item in the buffer. Thread-safe.
        /// </summary>
        /// <param name="value">The removed item, if available.</param>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <returns>True if an item was removed; otherwise, false.</returns>
        public bool TryPopFirst(out Titem? value, out string error) {

            bool aboutToBecomeEmpty = false;
            bool atLowerThreshold = false;
            bool popped = false;
            _collectionLock.EnterWriteLock();
            try {
                if (_CheckIfBufferIsEmpty( out error, out value)) {
                    return false;
                }

                aboutToBecomeEmpty = (_collection?.Count ?? 0) == 1;
                atLowerThreshold = (_collection?.Count ?? 0) == (_lowerThreshold +1)  && _lowerThreshold >= 0;

                value = _collection!.First!.Value!;

                _collection.RemoveFirst();
                error = string.Empty;
                popped = true;
                return popped;
            }
            catch (Exception ex) {
                error = $"Error popping first element: {ex.Message}";
                value = default;
                popped = false;
                return popped;
            }
            finally {      
                _collectionLock.ExitWriteLock();
                RaisePopItemEvents(popped, aboutToBecomeEmpty, atLowerThreshold);
            }
        }

        /// <summary>
        /// Attempts to peek at the last item in the buffer without removing it. Thread-safe.
        /// </summary>
        /// <param name="value">The last item, if available.</param>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <returns>True if an item was available; otherwise, false.</returns>
        public bool TryPeekLast(out Titem? value, out string error) {
            _collectionLock.EnterReadLock();
            try {

                if (_CheckIfBufferIsEmpty(out error, out value)) {
                    return false;
                }

                value = _collection!.Last!.Value;
                error = string.Empty;
                return true;
            }
            catch (Exception ex) {
                error = $"Error peeking last element: {ex.Message}";
                value = default;
                return false;
            }
            finally {
                _collectionLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Attempts to remove and return the last item in the buffer. Thread-safe.
        /// </summary>
        /// <param name="value">The removed item, if available.</param>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <returns>True if an item was removed; otherwise, false.</returns>
        public bool TryPopLast(out Titem? value, out string error) {

            bool aboutToBecomeEmpty = false;
            bool atLowerThreshold = false;
            bool popped = false;
            _collectionLock.EnterWriteLock();
            try {
                if (_CheckIfBufferIsEmpty(out error, out value)) {
                    return false;
                }

                aboutToBecomeEmpty = (_collection?.Count ?? 0) == 1;
                atLowerThreshold =(((_collection?.Count ?? 0) - _lowerThreshold) == 1) && (_lowerThreshold >= 0);

                value = _collection!.Last!.Value!;
                _collection.RemoveLast();
                error = string.Empty;

                popped = true;
                return popped;
            }
            catch (Exception ex) {
                error = $"Error popping last element: {ex.Message}";
                value = default;
                return popped;
            }
            finally {     
                _collectionLock.ExitWriteLock();
                RaisePopItemEvents(popped, aboutToBecomeEmpty, atLowerThreshold);
            }
        }


        /// <summary>
        /// Attempts to peek at the item at the specified index in the buffer without removing it. Thread-safe.
        /// </summary>
        /// <param name="index">The zero-based index of the item.</param>
        /// <param name="value">The item at the specified index, if available.</param>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <returns>True if the item was available; otherwise, false.</returns>
        public bool TryPeekAt(int index, out Titem value, out string error) {

            _collectionLock.EnterReadLock();
            value = default!;
            try {
                if (_CheckIfBufferIsEmpty(out error, out Titem item)) {                  
                    return false;
                }

                if (index < 0 || index >= _collection.Count) {
                    error = "Index out of range.";
                    value = default!;
                    return false;
                }

                var node = _collection.First;
                for (int i = 0; i < index; i++) {
                    if (node == null) {
                        error = "Node is null. Index out of range.";
                        value = default!;
                        return false;
                    }
                    node = node.Next;
                }

                if (node == null) {
                    error = "Node is null. Index out of range.";
                    value = default!;
                    return false;
                }

                value = node.Value;
                error = string.Empty;
                return true;
            }
            catch (Exception ex) {
                error = $"Error peeking element at index {index}: {ex.Message}";
                value = default!;
                return false;
            }
            finally {
                _collectionLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Attempts to remove and return the item at the specified index in the buffer. Thread-safe.
        /// </summary>
        /// <param name="index">The zero-based index of the item.</param>
        /// <param name="value">The removed item, if available.</param>
        /// <param name="error">Output error message if the operation fails.</param>
        /// <returns>True if the item was removed; otherwise, false.</returns>
        public bool TryPopAt(int index, out Titem value, out string error) {

            bool aboutToBecomeEmpty = false;
            bool atLowerThreshold = false;
            bool popped = false;

            _collectionLock.EnterWriteLock();
            try {
                if (_CheckIfBufferIsEmpty(out error, out value)) {
                    return false;
                }

                if (index < 0 || index >= _collection.Count) {
                    error = $"Index out of range. Indexacceptable range is: [0: {_collection.Count-1}], provided {index}.";
                    value = default!;
                    return false;
                }

                LinkedListNode<Titem>? node = _collection.First;
                for (int i = 0; i < index; i++) {
                    if (node == null) {
                        error = "Node is null. Index out of range.";
                        value = default!;
                        return false;
                    }
                    node = node.Next;
                }
                aboutToBecomeEmpty = (_collection?.Count ?? 0) == 1;
                atLowerThreshold = (_collection?.Count ?? 0) == _lowerThreshold && LowerThreshold >= 0;
                if (node == null) {
                    error = "Node is null. Index out of range.";
                    value = default!;
                    return false;
                }

                value = node.Value;

                _collection!.Remove(node);

                error = string.Empty;

                return true;
            }
            catch (Exception ex) {
                error = $"Error popping element at index {index}: {ex.Message}";
                value = default!;
                return false;
            }
            finally {
                
                _collectionLock.ExitWriteLock();
                RaisePopItemEvents(popped, aboutToBecomeEmpty, atLowerThreshold);
            }
        }

        #endregion Public Methods   
    }
}