using PissedEngineer.Primitives;
using PissedEngineer.HWControl;
using PissedEngineer.HWControl.Handlers;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PissedEngineer.ClickPLCHandler
{
    public interface IInterfaceConfiguration
    {
        InterfaceSelector Selector { get; set; }
        IEthernetConnectionConfiguration Network { get; set; }     
        ISerialPortConfiguration SerialPort { get; set; }
        bool CopyFrom(object src);
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class InterfaceConfiguration : ConfigurationBase, IInterfaceConfiguration
    {
        private InterfaceSelector _selector;
        private SerialPortConfiguration _serialPort;
        private IEthernetConnectionConfiguration _network;
        public InterfaceConfiguration() : base() {

            Selector = InterfaceSelector.Auto;
            SerialPort = null;
            Network = null;
        }

        public InterfaceConfiguration(IInterfaceConfiguration src) : this() {

            Selector = src.Selector;
            SerialPort = src.SerialPort;
            Network =  src.Network.Clone() as IEthernetConnectionConfiguration;
        }


        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public InterfaceSelector Selector {
            get => _selector;
            set => _selector = value;
        }

        [JsonProperty]
        public ISerialPortConfiguration SerialPort {
            get => _serialPort;
            set {
                if ((value as ISerialPortConfiguration) != null) {
                    _serialPort.CopyFrom(value);
                }
                else {
                    _serialPort = null;
                }
            }
        }

        [JsonProperty]
        public IEthernetConnectionConfiguration Network {
            get => _network;
            set {
                if((value as IEthernetConnectionConfiguration) != null) {
                    if(_network == null) {
                        _network = HwControlObjectFactory.CreateEthernetConnectionConfiguration(value);
                      
                    }   
                    _network.CopyFrom(value);
                }
                else {
                    _network = null;
                }   
            }
        }

        public override bool CopyFrom(object src) {

            var s = src as IInterfaceConfiguration;
 
            if (s == null) { return false; }

            SerialPort = null;
            Network = null;
            bool b1 = false;
            bool b2 = false;        

            Selector = s.Selector;

            if (s.SerialPort != null) {

                SerialPort =  HwControlObjectFactory.CreateSerialPortConfiguration();
                if (!(b1 = SerialPort.CopyFrom(s.SerialPort))) {
                    
                    SerialPort = null;
                }
            }

            if (s.Network != null) {

                var net = HwControlObjectFactory.CreateEthernetConnectionConfiguration();
                if( (b2 = net.CopyFrom(s.Network))) { 

                    Network = net;
                }
            }

            return b1 || b2;
        }

        public override object Clone() {
            var clone = new InterfaceConfiguration();
            clone.CopyFrom(this);
            return clone;
        }
    }
}
