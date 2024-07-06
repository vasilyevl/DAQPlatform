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

using PissedEngineer.HWControl;

namespace PissedEngineer.ClickPLCHandler
{
    public class TimerCounter
    {
        private RelayControlRO _timerCtrl;
        private RegisterInt16ControlRO _countsCtrl;
        private RegisterInt16Control _setPointCtrl;
        private RelayControl _resetCtrl;
        private bool _canWriteReset;
        private RelayControl _resetControl;

        public TimerCounter(RelayControlRO timerStateControl,
                RegisterInt16ControlRO counter,
                RegisterInt16Control setValueCtrl = null,
                RelayControl resetControl = null,
                bool canWriteReset = false) {

            _timerCtrl = timerStateControl;
            _countsCtrl = counter;
            _setPointCtrl = setValueCtrl;
            _resetControl = resetControl;
            _canWriteReset = canWriteReset;
        }

        public SwitchState GetState()
        {
            if (_timerCtrl != null) {

                if (_timerCtrl.Get(out SwitchState state)) {
                    return state;
                }
            }

            return new SwitchState(SwitchSt.Unknown);
        }

        public bool GetCounts(out ushort counts)
        {
            counts = ushort.MinValue;

            if (_countsCtrl != null) {

                return _countsCtrl.Get(out counts);
            }

            return false;
        }

        public bool Reset() => (_resetControl != null && _canWriteReset)
                ? _resetControl.Set(SwitchCtrl.Off) 
                    ? _resetControl.Set(SwitchCtrl.On) 
                    : false
                : false;

        public bool SetSetPoint(ushort value) => (_setPointCtrl != null) 
            ? _setPointCtrl.Set(value)
            : false;
           

        public bool GetSetPoint(out ushort value) {

            if (_setPointCtrl != null) {
                return _setPointCtrl.Get(out value);
            }
            value = 0;
            return false;
        }
          

    }
}
