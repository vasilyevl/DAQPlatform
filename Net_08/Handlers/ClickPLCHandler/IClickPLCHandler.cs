using Grumpy.HWControl.Common;

namespace Grumpy.ClickPLC
{
    public interface IClickPLCHandler
    {
        System.Boolean IsOpen { get; }

        System.Boolean Close();
        System.Boolean Init(ClickHandlerConfiguration cnfg);
        System.Boolean Init(System.String configJsonString);
        System.Boolean Open();
       // System.Boolean ReadDiscreteIO(System.String name, out SwitchSt status);
        System.Boolean ReadDiscreteIO(System.String name, out SwitchState state);
        System.Boolean ReadDiscreteIOs(System.String name, System.Int32 numberOfIosToRead, out SwitchState[] status);
        System.Boolean ReadRegister(System.String name, out System.UInt16 value);
        System.Boolean WriteDiscreteControl(System.String name, SwitchCtrl sw);
        System.Boolean WriteDiscreteControls(System.String startName, SwitchCtrl[] ctrls);
        System.Boolean WriteRegister(System.String name, System.UInt16 value);
    }
}