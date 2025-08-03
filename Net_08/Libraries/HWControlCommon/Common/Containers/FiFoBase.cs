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


namespace Grumpy.SDAQFramework.Common
{
    public class FIFOBase<TItem> : BufferBase<TItem>, IFIFOBase<TItem>
    {
        private const int _DefaultSize = 64;

        public FIFOBase() : this(_DefaultSize) { }

        public FIFOBase(int maxCapacity, string? name = null) : 
            base(maxCapacity, name) { }

        public FIFOBase(int maxCapacity, 
            TItem initialValue, 
            int upperThreshould, 
            int lowerThreshould, 
            string? name = null) :
            base(maxCapacity, initialValue, 
                upperThreshould, lowerThreshould, name)
        { }


        public virtual bool Push(TItem item, out string err) =>     
                                                 TryAdd(item, out err);
        public bool Pop(out TItem last, out string err) => 
                                                 TryPopFirst(out last!, out err);
        public bool Peek(out TItem last, out string err) => 
                                                 TryPeekFirst(out last!, out err);

        public bool HasPendingItems { get => !IsEmpty; }
    }
}


