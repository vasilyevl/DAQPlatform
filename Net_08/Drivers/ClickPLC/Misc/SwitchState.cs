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


namespace Grumpy.ClickPLCDriver
{
    public enum SwitchCtrl
    {
        On = 1,
        Off = 2,
    }

    public enum SwitchSt
    {
        Unknown = 0,
        On = 1,
        Off = 2
    }


    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class SwitchState
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