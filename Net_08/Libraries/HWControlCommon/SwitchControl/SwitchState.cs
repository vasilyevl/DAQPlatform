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
    public class SwitchState : ISwitchState
    {
        public SwitchState() {

            State = SwitchSt.Unknown;
        }

        public SwitchState(SwitchSt state) : this() {

            State = state;
        }

        [JsonProperty("state")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SwitchSt State { get; set; }

        public override bool Equals(object? other) =>
            ((other as SwitchState) is not null)
            && Equals(((SwitchState)other).State);


        public bool Equals(SwitchSt other) => State == other;


        public override int GetHashCode() =>
            base.GetHashCode() + 2 * State.GetHashCode();


        public override String ToString() {

            switch (State) {

                case (SwitchSt.On):
                    return nameof(SwitchSt.On);

                case (SwitchSt.Off):
                    return nameof(SwitchSt.Off);
            }

            return nameof(SwitchSt.Unknown);
        }
    }
}