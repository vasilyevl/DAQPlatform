using GSE.Common;

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;


namespace GSE.ClickPLCHandler
{
    public interface IClickHandlerConfiguration
    {
        IInterfaceConfiguration Interface { get; set; }
    }




    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ClickHandlerConfiguration : ConfigurationBase, IClickHandlerConfiguration
    {
        private IInterfaceConfiguration _interface;
        //private string _controlName;
        // private List<InputConfiguration> _inputs;
        //  private List<OutputConfiguration> _outputs;
        // private List<ControlRelayConfiguration> _controlRelays;

        public ClickHandlerConfiguration() : base() {

            _interface = new InterfaceConfiguration();
            //  _inputs = new List<InputConfiguration>();
            //  _outputs = new List<OutputConfiguration>();
            //  _controlRelays = new List<ControlRelayConfiguration>();
        }

        public override bool CopyFrom(object src) {
            var s = src as ClickHandlerConfiguration;

            if (s == null) { return false; }

            if (  s._interface != null) {

                var tmp = new InterfaceConfiguration();                  
                tmp.CopyFrom(s._interface);
                _interface = tmp;
            }
            /*
                        if ((s._inputs?.Count ?? 0) > 0) {

                            _inputs = s._inputs
                                .Select((x) => (InputConfiguration)x.Clone())
                                .ToList();
                        }
                        else {
                            _inputs = new List<InputConfiguration>();
                        }

                        if ((s._outputs?.Count ?? 0) > 0) {

                            _outputs = s._outputs
                                .Select((x) => (OutputConfiguration)x.Clone())
                                .ToList();
                        }
                        else {
                            _outputs = new List<OutputConfiguration>();
                        }

                        if ((s._controlRelays?.Count ?? 0) > 0) {

                            _controlRelays = s._controlRelays
                                .Select((x) => (ControlRelayConfiguration)x.Clone())
                                .ToList();
                        }
                        else {
                            _controlRelays = new List<ControlRelayConfiguration>();
                        }
                        */
            return true;
        }

        public override object Clone() {
            var clone = new ClickHandlerConfiguration();
            clone.CopyFrom(this);
            return clone;
        }

        [JsonProperty]
        public IInterfaceConfiguration Interface {
            get => _interface;
            set => _interface = value;
        }
        /*
        [JsonProperty]
        public List<InputConfiguration> Inputs {
            get => _inputs;
            set => _inputs = value;
        }

        [JsonProperty]
        public List<OutputConfiguration> Outputs {
            get => _outputs;
            set => _outputs = value;
        }

        [JsonProperty]
        public List<ControlRelayConfiguration> ControlRelays {
            get => _controlRelays;
            set => _controlRelays = value;
        }*/
    }
}
