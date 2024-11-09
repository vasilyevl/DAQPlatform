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


namespace Grumpy.StatePatternFramework
{
    public class FsmStack<T>
    {
        public const uint DEFAULT_CAPACITY = 16;
        public const uint MIN_CAPACITY = 2;
        public const string DEFAULT_NAME = "No name";

        private uint _maxCapacity;
        private LinkedList<T> _list;

        public FsmStack()
        {
            _maxCapacity = DEFAULT_CAPACITY;
            _list = new LinkedList<T>();
        }

        public FsmStack(uint maxCapacity = DEFAULT_CAPACITY) : this()
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
        
        public bool Pop(out T? last)
        {
            if (Peak( out last))
            {
                _list.RemoveLast();
                return true;
            }
            return false;
        }

        public bool Peak(out T? last)
        {
            LinkedListNode<T?> node = _list.Last!;

            if (node != null)
            {
                last = node.Value;
                return true;
            }
            last = default(T);
            return false;
        }



        public T[]? AsArray()
        {
            if (_list.Count > 0)
            {
                T[] array = _list.ToArray();
                array.Reverse();
                return array;
            }

            return null;
        }

        public List<T>? AsList()
        {
            if (_list.Count > 0)
            {
                T[]? array = AsArray();

                if (array is null) {
                    return null;
                }

                return new List<T>(array);
            }

            return null;
        }
    }
}
