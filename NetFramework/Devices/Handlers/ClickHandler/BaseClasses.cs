/*
Copyright (c) 2024 LV-PissedEngineer Permission is hereby granted, 
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

namespace PissedEngineer.ClickPLCHandler
{
    public delegate bool Write<TValue>(TValue value);
    public delegate bool Read<TValue>(out TValue value);

    public class ControlBase
    {
        public ControlBase(string name, IOType type, int len = 1, string endName = null)
        {
            _name = name;
            _type = type;
            _len = len;
            _endName = endName;
        }

        private string _name;
        public string Name => (string)_name?.Clone() ?? null;

        private string _endName;
        public string EndName => (string)_name?.Clone() ?? null;

        private int _len;
        public int Length => _len;

        private IOType _type;
        public IOType Type => _type;

    }


    public class ReadOnlyControlBase<TRead> : ControlBase
    {
        public ReadOnlyControlBase(string name, Read<TRead> rd, IOType type, int len = 1, string endName = null) :
        base(name, type, len, endName)
        {
            Get = rd;
        }

        public Read<TRead> Get { get; private set; }
    }


    public class WriteOnlyControlBase<TWrite> : ControlBase
    {
        public WriteOnlyControlBase(string name, Write<TWrite> rd, IOType type, int len = 1, string endName = null) :
        base(name, type, len, endName)
        {
            Set = rd;
        }

        public Write<TWrite> Set { get; protected set; }
    }


    public class ReadWriteControlBase<TWrite, TRead> : ControlBase
    {
        public ReadWriteControlBase(string name, Write<TWrite> wrt, Read<TRead> rd, IOType type, int len = 1, string endName = null) :
            base(name, type, len, endName)
        {
            Set = wrt;
            Get = rd;
        }

        public Write<TWrite> Set { get; protected set; }
        public Read<TRead> Get { get; protected set; }
    }


}
