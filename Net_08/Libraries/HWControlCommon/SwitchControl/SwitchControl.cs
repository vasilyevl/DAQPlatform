/*
 
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

using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.DaqFramework.IO
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class SwitchControl : IEquatable<object>, IEquatable<SwitchCtrl>,
        IEquatable<SwitchSt>
    {
        public SwitchControl() {
            Value = SwitchCtrl.Off;
        }

        public SwitchControl(SwitchCtrl ctrl) {
            Value = ctrl;
        }

        [JsonProperty("state")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SwitchCtrl Value { get; set; }

        public override bool Equals(object? other) {
            var o = other as SwitchControl;
            return (((object?)o) != null) ? Value == o.Value : false;
        }

        public bool Equals(SwitchCtrl other) => Value == other;

        public bool Equals(SwitchSt st) =>
            (Value == SwitchCtrl.On && st == SwitchSt.On)
            || (Value == SwitchCtrl.Off && st == SwitchSt.Off);

        public override int GetHashCode() =>
            base.GetHashCode() + 2 * Value.GetHashCode();
    }
}
