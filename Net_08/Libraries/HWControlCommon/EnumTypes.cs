using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System;

namespace Grumpy.HWControl.Common
{
    public enum InterfaceSelector {
        Auto = 0,
        Serial = 1,
        Network = 2
    }

    public enum SwitchCtrl {
        On = 1,
        Off =2,
    }

    public enum SwitchSt {
        Unknown = 0,
        On = 1,
        Off = 2
    }



    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class SwitchControl : IEquatable<object>, IEquatable<SwitchCtrl>,
        IEquatable<SwitchSt>
    {
        public SwitchControl()
        {
            Value = SwitchCtrl.Off;
        }

        public SwitchControl(SwitchCtrl ctrl)
        {
            Value = ctrl;
        }

        [JsonProperty("state")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SwitchCtrl Value { get; set; }

        public override bool Equals(object? other)
        {
            var o = other as SwitchControl;
            return (((object?)o) != null) ? Value == o.Value : false;
        }

        public bool Equals(SwitchCtrl other)=>  Value == other;

        public bool Equals(SwitchSt st) => 
            (Value == SwitchCtrl.On && st == SwitchSt.On) 
            || (Value == SwitchCtrl.Off && st == SwitchSt.Off);

        public override int GetHashCode()=> 
            base.GetHashCode() + 2 * Value.GetHashCode();
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class SwitchState : ISwitchState
    {
        public SwitchState() {

            State = SwitchSt.Unknown;
        }

        public SwitchState(SwitchSt state):this() {

            State = state;
        }

        [JsonProperty("state")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SwitchSt State { get; set; }

        public override bool Equals(object? other) =>
            ((other as SwitchState) is not null) 
            && Equals(((SwitchState)other).State);


        public bool Equals(SwitchSt other)  => State == other;
        

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
