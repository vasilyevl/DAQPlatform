using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PissedEngineer.Primitives.Stacks
{
    public interface IStack<T>
    {
        int StackID { get; }

        string Name { get; }

        int Count { get; }

        uint Capacity { get; }

        bool IsEmpty { get; }

        bool HasRoom { get; }

        int RoomLeft { get; }

        bool AtCapacity { get; }

        bool Push(T value, bool force = true);

        bool Pop(out T last);

        bool Peek(out T last);

        bool Clean();

        T[] PeekAllAsArray(bool recwntFirst);

        List<T> PeekAllAsList(bool recentFirst);
    }

    public class StackBase<T> : IStack<T>
    {
        public const uint MinCapacity = 2;

        private const uint _DefaultSize = 32;
        private const string _DefaultNamePreffix = "Stack_";

        private static int _objectCounter = 0;

        private string _name;
        private uint _maxCapacity;
        private LinkedList<T> _stack;
        private int _stackID;
        private object _stackLock;

        public StackBase()
        {
            _stackID = ++_objectCounter;

            _maxCapacity = _DefaultSize;
            _stack = new LinkedList<T>();
            _name = $"{_DefaultNamePreffix}{_stackID}";

            _stackLock = new object();
        }

        public StackBase(uint maxCapacity) : this()
        {
            Capacity = Math.Max(maxCapacity, MinCapacity);
        }

        public StackBase(uint maxCapacity, T initialValue) : this()
        {
            Capacity = Math.Max(maxCapacity, MinCapacity);
            Push(initialValue);
        }

        public StackBase(string name, uint maxCapacity = _DefaultSize) : this(maxCapacity)
        {
            _name = name;
        }

        public int StackID => _stackID;

        public string Name => _name;

        public int Count {
            get {
                lock (_stackLock) {
                    return _stack.Count;
                }
            }
        }

        public uint Capacity {
            get {
                uint result;
                Thread.MemoryBarrier();
                result = _maxCapacity;
                Thread.MemoryBarrier();
                return result;
            }
            set {
                Thread.MemoryBarrier();
                _maxCapacity = Math.Max(MinCapacity, value);
                Thread.MemoryBarrier();
            }
        }

        public bool IsEmpty => Count == 0;

        public int RoomLeft => (int)Math.Max(Capacity - Count, 0);

        public bool AtCapacity => Count >= Capacity;

        public bool HasRoom => !AtCapacity;

        public bool Push(T value, bool force = false)
        {
            lock (_stackLock) {

                try {

                    if (AtCapacity) {

                        if (force) {
                            while (AtCapacity) {
                                _stack.RemoveFirst();
                            }
                        }
                        else {
                            return false;
                        }
                    }
                    _stack.AddLast(value);
                    return true;
                }
                catch {
                    return false;
                }
            }
        }

        public bool Pop(out T last)
        {
            lock (_stackLock) {

                if (_Peak(out last)) {

                    _stack.RemoveLast();
                    return true;
                }
                return false;
            }
        }

        public bool Peek(out T last)
        {
            lock (_stackLock) {

                return _Peak(out last);
            }
        }

        private bool _Peak(out T last)
        {
            if (_stack.Count > 0) {
                LinkedListNode<T> node = _stack.Last;

                if (node != null) {
                    last = node.Value;
                    return true;
                }
                last = default(T);
                return false;
            }
            last = default(T);
            return false;
        }

        public bool Clean()
        {
            try {
                lock (_stackLock) {

                    if (!IsEmpty) {

                        while (!IsEmpty) {

                            _stack.RemoveLast();
                        }
                        return true;
                    }
                    return true;
                }
            }
            catch {
                return false;
            }
        }

        public T[] PeekAllAsArray(bool recentFirst = true)
        {
            lock (_stackLock) {

                if (_stack.Count > 0) {
                    T[] array = _stack.ToArray();
                    if (recentFirst) {
                        Array.Reverse(array);
                    }
                    return array;
                }

                return null;
            }
        }

        public List<T> PeekAllAsList(bool recentFirst = true)
        {
            if (_stack.Count > 0) {

                T[] array = PeekAllAsArray( recentFirst );

                List<T> list = new List<T>();

                foreach (var item in array) {
                    list.Add(item);
                }

                return list;
            }

            return null;
        }
    }
}
