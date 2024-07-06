using Grumpy.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.HWControl.Common.Handlers
{
    public interface IEthernetConnectionConfiguration
    {
        public Int32 DataPort { get; set; }
        public String IpAddress { get; set; }
        public Int32 MessagePort { get; set; }
        public String Name { get; set; }
        public Int32 Port { get; set; }
        public Int32 Timeout { get; set; }

        public Boolean CopyFrom(Object src);
        public void Reset();
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class EthernetConnectionConfiguration : ConfigurationBase, IEthernetConnectionConfiguration
    {
        public const int DefaultTimeoutMs = 5000;
        public const string DefaultIP = "0:0:0:0";

        public const int DefaultPort = 0;


        private string? _name;
        private string? _ipAddress;
        private int _port;
        private int _dataPort;
        private int _messagePort;
        private int _timeout;

        public EthernetConnectionConfiguration() {
            Timeout = DefaultTimeoutMs;
        }

        [JsonProperty]
        public string Name {
            get { return (string?)_name?.Clone()! ?? null!; }
            set { SetProperty(ref _name, (string)value?.Clone()! ?? null!, () => Name); }
        }

        [JsonProperty]
        public string IpAddress {
            get { return (string)_ipAddress?.Clone()! ?? null!; }
            set { SetProperty(ref _ipAddress, (string)value?.Clone()! ?? null!, () => IpAddress); }
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

        public override void Reset() {
            Timeout = DefaultTimeoutMs;
            IpAddress = DefaultIP;
            Port = DefaultPort;
            MessagePort = DefaultPort;
            DataPort = DefaultPort;
        }

        public override bool CopyFrom(object src) {
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
