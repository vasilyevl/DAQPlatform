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
*/using PissedEngineer.HWControl;

namespace PissedEngineer.ClickPLCHandler
{
    public class RelayControl : ReadWriteControlBase<SwitchCtrl, SwitchState>
    {
        public RelayControl(string name, Write<SwitchCtrl> wrt, Read<SwitchState> rd) :
            base(name, wrt, rd, IOType.ControlRelay)
        {
        }
    }

    public class RelayControlRO : ReadOnlyControlBase<SwitchState>
    {
        public RelayControlRO(string name, Read<SwitchState> rd) :
            base(name, rd, IOType.ControlRelay)
        { }
    }

    public class RelayArrayControl : ReadWriteControlBase<SwitchCtrl[], SwitchState[]>
    {
        public RelayArrayControl(string name, Write<SwitchCtrl[]> wrt, Read<SwitchState[]> rd) :
            base(name, wrt, rd, IOType.ControlRelay)
        { }
    }


    public class RegisterInt16Control : ReadWriteControlBase<ushort, ushort>
    {
        public RegisterInt16Control(string name, Write<ushort> wrt, Read<ushort> rd) :
            base(name, wrt, rd, IOType.RegisterInt16)
        { }
    }

    public class RegisterInt16ControlRO : ReadOnlyControlBase<ushort>
    {
        public RegisterInt16ControlRO(string name, Read<ushort> rd) :
            base(name, rd, IOType.RegisterInt16)
        { }
    }
}
