using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.HWControl.IO
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
