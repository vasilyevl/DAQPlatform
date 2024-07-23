/*
 * Copyright (c) 2024 Grumpy. Permission is hereby granted, 
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
using Newtonsoft.Json;


namespace Grumpy.ClickPLCHandler
{
    public interface ITcpIpConnectionConfiguration
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
    public class TcpIpConnectionConfiguration : ITcpIpConnectionConfiguration, ICloneable
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

        public TcpIpConnectionConfiguration() {
            Timeout = DefaultTimeoutMs;
        }

        [JsonProperty]
        public string Name {
            get { return (string?)_name?.Clone()! ?? null!; }
            set { _name =  (string)value?.Clone()! ?? null!; }
        }

        [JsonProperty]
        public string IpAddress {
            get { return (string)_ipAddress?.Clone()! ?? null!; }
            set { _ipAddress = (string)value?.Clone()! ?? null!; }
        }

        [JsonProperty]
        public int Port {
            get { return _port; }
            set { _port = value; }
        }

        [JsonProperty]
        public int DataPort {
            get { return _dataPort; }
            set { _dataPort = value; }
        }

        [JsonProperty]
        public int MessagePort {
            get { return _messagePort; }
            set { _messagePort = value; }
        }


        [JsonProperty]
        public int Timeout {
            get { return _timeout; }
            set { _timeout = value; }
        }

        public void Reset() {
            Timeout = DefaultTimeoutMs;
            IpAddress = DefaultIP;
            Port = DefaultPort;
            MessagePort = DefaultPort;
            DataPort = DefaultPort;
        }

        public  bool CopyFrom(object src) {
            var s = src as TcpIpConnectionConfiguration;

            if (s == null) { return false; }

            Timeout = s.Timeout;
            IpAddress = s.IpAddress;
            Port = s.Port;
            MessagePort = s.MessagePort;
            DataPort = s.DataPort;

            return true;
        }

        public object Clone() {
            var clone = new TcpIpConnectionConfiguration();
            clone.CopyFrom(this);
            return clone;
        }

    }
}
