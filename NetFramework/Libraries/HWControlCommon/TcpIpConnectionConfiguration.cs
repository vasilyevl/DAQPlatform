using Newtonsoft.Json;

using PissedEngineer.Primitives;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PissedEngineer.HWControl.Handlers
{



    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    internal class EthernetConnectionConfiguration : ConfigurationBase, IEthernetConnectionConfiguration
    {
        public const int DefaultTimeoutMs = 5000;
        public const string DefaultIP = "0:0:0:0";

        public const int DefaultPort = 0;


        private string _name;
        private string _ipAddress;
        private int _port;
        private int _dataPort;
        private int _messagePort;
        private int _timeout;

        internal EthernetConnectionConfiguration() {
            Timeout = DefaultTimeoutMs;
        }

        internal EthernetConnectionConfiguration(IEthernetConnectionConfiguration src):this() {
            CopyFrom(src);
        }

        [JsonProperty]
        public string Name {
            get { return (string)_name?.Clone() ?? null; }
            set { SetProperty(ref _name, (string)value?.Clone() ?? null, () => Name); }
        }

        [JsonProperty]
        public string IpAddress {
            get { return (string)_ipAddress?.Clone() ?? null; }
            set { SetProperty(ref _ipAddress, (string)value?.Clone() ?? null, () => IpAddress); }
        }

        [JsonProperty]
        public int Port {
            get { return _port; }
            set { SetProperty(ref _port, value, () => Port); }
        }

        [JsonProperty]
        public int DataPort {
            get { return _dataPort; }
            set { SetProperty(ref _dataPort, value, () => DataPort); }
        }

        [JsonProperty]
        public int MessagePort {
            get { return _messagePort; }
            set { SetProperty(ref _messagePort, value, () => MessagePort); }
        }

        [JsonProperty]
        public int Timeout {
            get { return _timeout; }
            set { SetProperty(ref _timeout, value, () => Timeout); }
        }

        public void SetToDefaults() {
            Timeout = DefaultTimeoutMs;
            IpAddress = DefaultIP;
            Port = DefaultPort;
            MessagePort = DefaultPort;
            DataPort = DefaultPort;
        }

        public override bool CopyFrom(object src) {
            var s = src as IEthernetConnectionConfiguration;
            return CopyFrom(s); 
        }

        public bool CopyFrom(IEthernetConnectionConfiguration src) {
            if (src == null) { return false; }

            Timeout = src.Timeout;
            IpAddress = src.IpAddress;
            Port = src.Port;
            MessagePort = src.MessagePort;
            DataPort = src.DataPort;

            return true;
        } 
    }
}
