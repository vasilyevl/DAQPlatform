using System;
using System.Collections.Generic;
using System.Linq;

namespace FSM
{
    public class Stack<T>
    {
        public const uint DEFAULT_CAPACITY = 16;
        public const uint MIN_CAPACITY = 2;
        public const string DEFAULT_NAME = "No name";

        private uint _maxCapacity;
        private LinkedList<T> _list;

        public Stack()
        {
            _maxCapacity = DEFAULT_CAPACITY;
            _list = new LinkedList<T>();
        }

        public Stack(uint maxCapacity = DEFAULT_CAPACITY) : this()
        {
            _maxCapacity = Math.Max(maxCapacity, MIN_CAPACITY);
        }


        public void Push(T value)
        {
            while (_list.Count >= _maxCapacity)
                _list.RemoveFirst();

            _list.AddLast(value);
        }

        public int Count 
        { get { return _list.Count; } }
        
        public int Capacity 
        { get { return (int)_maxCapacity; } }
        
        public bool Pop(out T last)
        {
            if (Peak( out last))
            {
                _list.RemoveLast();
                return true;
            }
            return false;
        }

        public bool Peak(out T last)
        {
            LinkedListNode<T> node = _list.Last;
            if (node != null)
            {
                last = node.Value;
                return true;
            }
            last = default(T);
            return false;
        }



        public T[] AsArray()
        {
            if (_list.Count > 0)
            {
                T[] array = _list.ToArray();
                array.Reverse();
                return array;
            }

            return null;
        }

        public List<T> AsList()
        {
            if (_list.Count > 0)
            {
                T[] array = AsArray();
         
                List<T> list = new List<T>();

                foreach (var item in array)
                    list.Add(item);

                return list;
            }

            return null;
        }
    }
}
