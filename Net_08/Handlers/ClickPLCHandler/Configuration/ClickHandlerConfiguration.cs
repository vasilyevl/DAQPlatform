


using Grumpy.HWControl.Configuration;
using Newtonsoft.Json;


namespace Grumpy.ClickPLCHandler
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
