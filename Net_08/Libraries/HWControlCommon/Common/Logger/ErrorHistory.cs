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

namespace Grumpy.DAQFramework.Common
{
    public class ErrorHistory: StackBase<LogRecord> {

        public static ErrorHistory Create(string? name = null,
            uint maxCapacity = _MaxCapacity) {

            return new ErrorHistory(name, maxCapacity);
        }

        private static int _objectCounter = 1;
        private const int _MaxCapacity = 16;
        public ErrorHistory() : base($"ErrorStack_{_objectCounter}") {

            _objectCounter++;
        }

        public ErrorHistory(string? name = null, 
            uint maxCapacity = _MaxCapacity ) : 
            base( string.IsNullOrEmpty(name) ? $"ErrorStack_{_objectCounter}" : name,
                maxCapacity) {
            _objectCounter++;
        }

    }
}
