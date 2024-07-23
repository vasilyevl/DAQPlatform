/*
 
Copyright (c) 2024 Grumpy. Permission is hereby granted, 
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

    internal enum IOType
    {
        Unknown,
        Input,
        InputRegister,
        InputURegister,
        Output,
        OutputRegister,
        OutputURegister,
        ControlRelay,
        RegisterInt16,
        RegisterInt32,
        RegisterHex,
        RegisterFloat32,
        Timer,
        TimerRegister,
        Counter,
        CounterRegister,
        SystemControlRelay,
        SystemRegister,
        Text
    }
    internal static class ChannelConstants
    {
        private static IReadOnlyDictionary<string, IOType> _ioTypes =
            new Dictionary<string, IOType>() {
                {"X", IOType.Input },
                {"Y", IOType.Output},
                {"C", IOType.ControlRelay},
                {"T", IOType.Timer},
                {"CT", IOType.Counter},
                {"SC", IOType.SystemControlRelay},
                {"DS", IOType.RegisterInt16},
                {"DD", IOType.RegisterInt32},
                {"DH", IOType.RegisterHex},
                {"DF", IOType.RegisterFloat32},
                {"XD", IOType.InputRegister},
                {"YD", IOType.OutputRegister},
                {"TD", IOType.TimerRegister},
                {"CTD", IOType.CounterRegister},
                {"SD", IOType.SystemRegister},
                {"TXT", IOType.Text},
                {"XDU", IOType.InputURegister},
                {"YDU", IOType.OutputURegister}
            };

        internal static IReadOnlyDictionary<string, IOType> IoTypes {
            get {
                var res = new Dictionary<string, IOType>();
                foreach (var kv in _ioTypes) {
                    res.Add((string)kv.Key.Clone(), kv.Value);
                }
                return res;
            }
        }

        public static List<string> ValidControlNamePrefixes =>
            new List<string>(_ioTypes.Keys);
    }
}
