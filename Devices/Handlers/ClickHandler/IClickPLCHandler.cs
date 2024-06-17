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
    public interface IClickPLCHandler
    {
        System.Boolean IsOpen { get; }

        System.Boolean Close();
        System.Boolean Init(IClickHandlerConfiguration cnfg);
        //System.Boolean Init(System.String configJsonString);
        System.Boolean Open();
        System.Boolean ReadDiscreteIO(System.String name, out SwitchSt status);
        System.Boolean ReadDiscreteIO(System.String name, out SwitchState state);
        System.Boolean ReadDiscreteIOs(System.String name, System.Int32 numberOfIosToRead, out SwitchState[] status);
        System.Boolean ReadRegister(System.String name, out System.UInt16 value);
        System.Boolean WriteDiscreteControl(System.String name, SwitchCtrl sw);
        System.Boolean WriteDiscreteControls(System.String startName, SwitchCtrl[] ctrls);
        System.Boolean WriteRegister(System.String name, System.UInt16 value);
    }
}