using LV.HWControl.Common;

namespace LV.ClickPLCHandler
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
