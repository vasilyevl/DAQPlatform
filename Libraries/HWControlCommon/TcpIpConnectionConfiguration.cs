using GSE.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSE.HWControl.Common.Handlers
{
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class EthernetConnectionConfiguration : ConfigurationBase
    {
        public const int DefaultTimeputMs = 5000;
        public const string DefaultIP = "0:0:0:0";

        public const int  DefaultPort = 0;


        private string _name;
        private string _ipAddress;
        private int _port;
        private int _dataPort;
        private int _messagePort;
        private int _timeout;

        public EthernetConnectionConfiguration ()
        {
            Timeout = DefaultTimeputMs;
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


        public void SetToDefaults()
        {
            Timeout = DefaultTimeputMs;
            IpAddress = DefaultIP;
            Port = DefaultPort;
            MessagePort = DefaultPort;
            DataPort = DefaultPort;
        }

        public override bool CopyFrom(object src)
        {
            var s = src as EthernetConnectionConfiguration;

            if (s == null) { return false; }

            Timeout = s.Timeout;
            IpAddress = s.IpAddress;
            Port = s.Port;
            MessagePort = s.MessagePort;
            DataPort = s.DataPort;

            return true;
        }
    }
}
