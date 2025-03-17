using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Grumpy.Common
{
    public interface ILifo<T>
    {
        int ID { get; }
        string Name { get; }
        int CurrentCount { get; }
        uint Capacity { get; }
        bool IsEmpty { get; }
        bool HasRoom { get; }
        int RoomLeft { get; }
        bool AtCapacity { get; }
        bool Push(T value, bool force = true);
        bool Pop(out T last);
        bool Peek(out T last);
        bool Purge();
        T[] PeekAllAsArray(bool recentFirst);
        List<T> PeekAllAsList(bool recentFirst);
    }

    public class LifoBase<T> : ILifo<T>
    {
        public const uint MinCapacity = 2;
        private const uint DefaultSize = 32;
        private const string DefaultNamePrefix = "LIFO_";
        private static int objectCounter = 0;

        private readonly string name;
        private uint maxCapacity;
        private readonly LinkedList<T> lifo;
        private readonly int lifoID;
        private readonly ReaderWriterLockSlim lifoLock;

        public LifoBase() : this(DefaultSize) { }

        public LifoBase(uint maxCapacity) {

            lifoID = Interlocked.Increment(ref objectCounter);
            this.maxCapacity = Math.Max(maxCapacity, MinCapacity);
            lifo = new LinkedList<T>();
            name = $"{DefaultNamePrefix}{lifoID}";
            lifoLock = new ReaderWriterLockSlim();
        }

        public LifoBase(uint maxCapacity, T initialValue) :
            this(maxCapacity) {
            Push(initialValue);
        }

        public LifoBase(string name, uint maxCapacity = DefaultSize) : 
            this(maxCapacity) {
            this.name = name;
        }


        public int ID => lifoID;
        public string Name => name;

        public int CurrentCount {
            get {
                lifoLock.EnterReadLock();
                try {
                    return lifo.Count;
                }
                finally {
                    lifoLock.ExitReadLock();
                }
            }
        }

        public uint Capacity {
            get {
                lifoLock.EnterReadLock();
                try {
                    return maxCapacity;
                }
                finally {
                    lifoLock.ExitReadLock();
                }
            }
            set {
                lifoLock.EnterWriteLock();
                try {
                    maxCapacity = Math.Max(MinCapacity, value);
                }
                finally {
                    lifoLock.ExitWriteLock();
                }
            }
        }

        public bool IsEmpty => CurrentCount == 0;
        
        public int RoomLeft => (int)Math.Max(Capacity - CurrentCount, 0);

        public bool AtCapacity => CurrentCount >= Capacity;

        public bool HasRoom => !AtCapacity;

        private string lastError = string.Empty;

        public string LastError {
            get { 
                lifoLock.EnterReadLock();
                try {
                    return lastError;
                }
                catch (Exception ex) {
                    return ex.Message;
                }
                finally {
                    lifoLock.ExitReadLock();
                }
            }
            private set {
                lifoLock.EnterReadLock();
                try {
                    lastError = value;
                }
                catch (Exception ex) {
                    lastError = ex.Message;
                }
                finally {
                    lifoLock.ExitReadLock();
                }
            } 
        }

        public bool Push(T value, bool force = false) {

            lifoLock.EnterWriteLock();

            try {

                if (AtCapacity) {

                    if (force) {

                        while (AtCapacity) {
                            lifo.RemoveFirst();
                        }
                    }
                    else {

                        return false;
                    }
                }

                lifo.AddLast(value);
                return true;
            }
            catch (Exception) {

                return false;
            }
            finally {
                lifoLock.ExitWriteLock();
            }
        }

        public bool Pop(out T last) {
            lifoLock.EnterWriteLock();
            try {
                if (Peek(out last)) {
                    lifo.RemoveLast();
                    return true;
                }
                return false;
            }
            finally {
                lifoLock.ExitWriteLock();
            }
        }

        public bool Peek(out T last) {
            lifoLock.EnterReadLock();
            try {
                return TryPeek(out last);
            }
            finally {
                lifoLock.ExitReadLock();
            }
        }

        private bool TryPeek(out T last) {

            if (lifo.Count > 0) {
            
                last = lifo.Last.Value;
                return true;
            }
            last = default;
            return false;
        }

        public bool Purge() {
            lifoLock.EnterWriteLock();
            try {
                lifo.Clear();
                return true;
            }
            catch (Exception) {
                return false;
            }
            finally {
                lifoLock.ExitWriteLock();
            }
        }

        public T[] PeekAllAsArray(bool recentFirst = true) {
            lifoLock.EnterReadLock();
            try {
                if (lifo.Count > 0) {
                    T[] array = lifo.ToArray();
                    if (recentFirst) {
                        Array.Reverse(array);
                    }
                    return array;
                }
                return Array.Empty<T>();
            }
            finally {
                lifoLock.ExitReadLock();
            }
        }

        public List<T> PeekAllAsList(bool recentFirst = true) {
            return PeekAllAsArray(recentFirst).ToList();
        }
    }


 


}