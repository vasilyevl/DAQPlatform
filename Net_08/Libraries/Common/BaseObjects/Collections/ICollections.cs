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

namespace Grumpy.Common.BaseObjects.Collections
{
    /// <summary>
    /// Base interface for collection types.
    /// </summary>
    /// <typeparam name="TItem">The type of elements in the collection.</typeparam>
    public interface IBufferBase<TItem>: IDisposable
    {

        public event EventHandler? HasBecomeEmpty;
        public event EventHandler? HasReachedCapacity;
        public event EventHandler? DroppedBelowLowerThreshold;
        public event EventHandler? WentOverUpperThreshold;
        public event EventHandler<int>? ItemAdded;
        public event EventHandler<int>? ItemsDiscarded;
        public int AvailableCapacity { get; }
        public int Count { get; }
        public bool HasRoom { get; }
        public int ID { get; }
        public bool IsAtCapacity { get; }
        public bool IsEmpty { get; }
        public int MaxCapacity { get; set; }
        public string Name { get; }
        public bool TryAdd(TItem value, out string error);
        public bool TryClear(out string error);
        public void Dispose();
        public bool TryMakeRoom(int requiredCapacity, out int removedItemsCount, out string error, bool removeFromFirst = true);
        public bool TryInsertAt(int index, TItem value, out string error);
        public TItem[] PeekAllAsArray(out string error, bool recentFirst = false);
        public List<TItem> PeekAllAsList(out string error, bool recentFirst = false);
        public bool TryPeekAt(int index, out TItem value, out string error);
        public bool TryPeekLast(out TItem? value, out string error);
        public bool TryPopAt(int index, out TItem value, out string error);
        public bool TryPopLast(out TItem? value, out string error);
    }

    /// <summary>
    /// Interface for a Last-In-First-Out (LIFO) collection.
    /// </summary>
    /// <typeparam name="T">The type of elements in the LIFO collection.</typeparam>
    public interface ILIFO<TItem> : IBufferBase<TItem>
    {

        /// <summary>
        /// Pops the most recently added element from the LIFO collection.
        /// </summary>
        /// <param name="last">The popped element.</param>
        /// <returns>True if an element was successfully popped; otherwise, false.</returns>
        bool Pop(out TItem last, out string err);

        /// <summary>
        /// Peeks at the most recently added element without removing it.
        /// </summary>
        /// <param name="last">The most recently added element.</param>
        /// <returns>True if an element was successfully peeked; otherwise, false.</returns>
        bool Peek(out TItem last, out string err);
    }

    public interface IFIFOBase<TItem> : IBufferBase<TItem>
    {
        /// <summary>
        /// Checks whether the FIFO collection has pending items.
        /// Returs true if there are pending items; otherwise, false.
        /// </summary>
        bool HasPendingItems { get; }

        /// <summary>
        /// Pops the most recently added element from the LIFO collection.
        /// </summary>
        /// <param name="last">The popped element.</param>
        /// <returns>True if an element was successfully popped; otherwise, false.</returns>
        bool Pop(out TItem last, out string err);

        /// <summary>
        /// Peeks at the most recently added element without removing it.
        /// </summary>
        /// <param name="last">The most recently added element.</param>
        /// <returns>True if an element was successfully peeked; otherwise, false.</returns>
        bool Peek(out TItem last, out string err);


    }

    public interface IActiveFifo<TObject> : IFIFOBase<TObject>
    {
        bool ReceiverIsSet { get; }
        bool ReceiverIsOn { get; }
        bool ReceiverIsInternallyPaused { get; }
        bool ReceiverIsPaused { get; }
        bool UsePause { get; }
        bool ReceiverThreadAlive { get; }

        bool SetReceiver(DataReceiver<TObject> processor);
        bool PauseReceiver();
        bool ResumeReceiver();
    }
}
