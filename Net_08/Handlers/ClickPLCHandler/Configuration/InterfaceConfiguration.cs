﻿using Grumpy.Common;
using Grumpy.HWControl.Common;
using Grumpy.HWControl.Common.Handlers;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Grumpy.ClickPLC
{
    public interface IInterfaceConfiguration
    {
        InterfaceSelector Selector { get; set; }
        EthernetConnectionConfiguration? Network { get; set; }     
        SerialPortConfiguration? SerialPort { get; set; }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class InterfaceConfiguration : ConfigurationBase, IInterfaceConfiguration
    {
        private InterfaceSelector _selector;
        private SerialPortConfiguration? _serialPort;
        private EthernetConnectionConfiguration? _network;
        public InterfaceConfiguration() : base() {}

        public InterfaceConfiguration(IInterfaceConfiguration src) : this() {

            Selector = src.Selector;
            SerialPort = src.SerialPort;
            Network =  (src?.Network is not null) ? (src.Network.Clone() as EthernetConnectionConfiguration) : null;
        }




        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public InterfaceSelector Selector {
            get => _selector;
            set => _selector = value;
        }

        [JsonProperty]
        public SerialPortConfiguration? SerialPort {
            get => _serialPort;
            set => _serialPort = value;
        }

        [JsonProperty]
        public EthernetConnectionConfiguration? Network {
            get => _network;
            set => _network = value;
        }




        internal bool CopyFrom( IInterfaceConfiguration s) {

            Selector = s.Selector;

            SerialPort = null;
            Network = null;
            bool b1 = true;
            bool b2 = true;

            try {

                if (s.SerialPort is not null) {

                    var sp = new SerialPortConfiguration();
                    b1 = sp.CopyFrom(s.SerialPort);
                    if (b1) { SerialPort = sp; }
                }

                if (s.Network != null) {

                    var net = new EthernetConnectionConfiguration();
                    b2 = net.CopyFrom(s.Network);
                    if (b2) { Network = net; }
                }
                return b1 && b2;
            } 
            catch {

                return false;
            } 
        }


        public override bool CopyFrom(object src) {
            var s = src as IInterfaceConfiguration;

            return  (s is null) ? false : CopyFrom(s);
        }


        public override void Reset() {
            Selector = InterfaceSelector.Auto;
            SerialPort = null;
            Network = null;
        }
    }
}