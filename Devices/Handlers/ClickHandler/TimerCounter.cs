using LV.HWControl.Common;

namespace LV.ClickPLCHandler
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
                bool canWriteReset = false)

        {
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

        public bool Reset() =>
             (_resetControl != null && _canWriteReset)
                ? _resetControl.Set(SwitchCtrl.Off) 
                    ? _resetControl.Set(SwitchCtrl.On) 
                    : false
                : false;

        public bool SetSetPoint(ushort value) =>

            (_setPointCtrl != null) 
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
