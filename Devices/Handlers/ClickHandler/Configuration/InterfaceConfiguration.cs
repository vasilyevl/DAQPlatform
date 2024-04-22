using LV.Common;
using LV.HWControl.Common;
using LV.HWControl.Common.Handlers;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LV.ClickPLCHandler
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class InterfaceConfiguration : ConfigurationBase
    {
        private InterfaceSelector _selector;
        private SerialPortConfiguration _serialPort;
        private EthernetConnectionConfiguration _network;
        public InterfaceConfiguration() : base()
        {

            Selector = InterfaceSelector.Auto;
            SerialPort = null;
            Network = null;
        }

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public InterfaceSelector Selector
        {
            get => _selector;
            set => _selector = value;
        }

        [JsonProperty]
        public SerialPortConfiguration SerialPort
        {
            get => _serialPort;
            set => _serialPort = value;
        }

        [JsonProperty]
        public EthernetConnectionConfiguration Network
        {
            get => _network;
            set => _network = value;
        }

        public override bool CopyFrom(object src)
        {
            var s = src as InterfaceConfiguration;

            if (s == null) { return false; }

            Selector = s.Selector;

            SerialPort = null;
            Network = null;
            bool b1 = false;
            bool b2 = false;

            if (s.SerialPort != null)
            {

                var sp = new SerialPortConfiguration();
                b1 = sp.CopyFrom(s.SerialPort);
                if (b1)
                {
                    SerialPort = sp;
                }
            }

            if (s.Network != null)
            {

                var net = new EthernetConnectionConfiguration();
                b2 = net.CopyFrom(s.Network);
                if (b2)
                {
                    Network = net;
                }
            }

            return b1 || b2;
        }
    }
}
