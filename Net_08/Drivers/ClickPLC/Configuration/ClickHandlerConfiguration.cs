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

using Grumpy.DAQFramework.Configuration;
using Newtonsoft.Json;


namespace Grumpy.ClickPLCDriver
{
    public interface IClickHandlerConfiguration
    {
        IInterfaceConfiguration? Interface { get; set; }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ClickHandlerConfiguration : IClickHandlerConfiguration, ICloneable
    {
        private InterfaceConfiguration? _interface;
        //private string _controlName;
        // private List<InputConfiguration> _inputs;
        //  private List<OutputConfiguration> _outputs;
        // private List<ControlRelayConfiguration> _controlRelays;

        public ClickHandlerConfiguration() : base() {

            Interface = new InterfaceConfiguration();   
        }

        public ClickHandlerConfiguration(ClickHandlerConfiguration source) : this() {

            this.CopyFrom(source);
        }

        public bool CopyFrom(object src) {

            var s = src as IClickHandlerConfiguration;

            if (s == null) { return false; }

            if (s.Interface != null) {

                _interface = null;
                var tmp = new InterfaceConfiguration();
                tmp.CopyFrom(s.Interface);
                _interface = tmp;
            }
            return true;
        }

        public void Reset() {

            _interface = new InterfaceConfiguration();
        }

        public object Clone() {

            var clone = new ClickHandlerConfiguration();
            clone.CopyFrom(this);
            return clone;
        }

        [JsonProperty]
        public IInterfaceConfiguration? Interface {
            get => _interface;
            set => _interface = value as InterfaceConfiguration;
        }
    }
}
