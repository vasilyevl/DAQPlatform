/*Copyright (c) 2024 vasilyevl (Grumpy). Permission is hereby granted, 
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

namespace Grumpy.ClickPLCHandler
{
    public interface IClickPLCHandler
    {
        Boolean IsOpen { get; }
        ILogRecord? LastRecord { get; }

        Boolean Open();
        Boolean Close();

        Boolean Init(String configJsonString);

        Boolean ReadDiscreteControl(String name, out SwitchState state);
        Boolean WriteDiscreteControl(String name, SwitchCtrl sw);

        Boolean ReadDiscreteControls(String name, int numberOfIosToRead, out SwitchState[] status);
        Boolean WriteDiscreteControls(String startName, SwitchCtrl[] controls);

        Boolean ReadInt16Register(String name, out short value);
        Boolean WriteInt16Register(String name, short value);

        Boolean ReadUInt16Register(String name, out ushort value);
        Boolean WriteUInt16Register(String name, ushort value);

        Boolean ReadFloat32Register(String name, out float value);
        Boolean WriteFloat32Register(String name, float value);




    }
}