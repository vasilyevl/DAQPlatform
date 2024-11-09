﻿/* 
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

namespace Grumpy.StatePatternFramework
{
    public class ControlQueue<T> : ConcurrentQueue<T> where T : new()
    {
        public const int DEFAULT_LENGTH = 1024;
        public const int INFINIT_LENGTH = -1;

        protected int _maxLength;
        protected T? _lastDequeValue;

        public ControlQueue(T startUpValue) : base()
        {
            _maxLength = INFINIT_LENGTH;
            _lastDequeValue = startUpValue;
        }

        public T? Value
        {
            get
            {
                T? vdq;
                if (TryDequeue(out vdq))
                    System.Threading.Thread.MemoryBarrier();
                _lastDequeValue = vdq ;
                System.Threading.Thread.MemoryBarrier();
                return _lastDequeValue;
            }
        }
    }
}
