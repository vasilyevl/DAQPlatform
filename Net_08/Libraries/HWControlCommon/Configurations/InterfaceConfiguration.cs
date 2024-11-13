/*
 * Copyright (c) 2024 vasilyevl (Grumpy). Permission is hereby granted, 
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

using DAQFramework.Common.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Grumpy.DAQFramework.Configuration
{
    public interface IInterfaceConfiguration
    {
        InterfaceSelector ActiveInterface { get; set; }
        TcpIpConnectionConfiguration? Network { get; set; }
        SerialPortConfiguration? SerialPort { get; set; }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class InterfaceConfiguration :  IInterfaceConfiguration
    {
        private const InterfaceSelector _DefaultInterface = 
                                        InterfaceSelector.Auto;


        private TcpIpConnectionConfiguration? _network;
        private SerialPortConfiguration? _serialPort;
        private InterfaceSelector _activeInterface;

        public InterfaceConfiguration() : base() { 

            ActiveInterface = _DefaultInterface;
            Network = null;
            SerialPort = null;
        }

        public InterfaceConfiguration(IInterfaceConfiguration src) : this() {

            Network = (src?.Network is not null) ? 
                (src.Network.Clone( out string? error) as TcpIpConnectionConfiguration) : 
                null;
        }

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public InterfaceSelector ActiveInterface {
            get => _activeInterface;
            set => _activeInterface = value;
        }

        [JsonProperty]
        public TcpIpConnectionConfiguration? Network {
            get => _network;
            set => _network = value;
        }

        [JsonProperty]
        public SerialPortConfiguration? SerialPort {
           get => _serialPort;
           set => _serialPort = value;
        }

        internal bool CopyFrom(IInterfaceConfiguration s) {

            Network = null;
            bool b1 = true;
            bool b2 = true;

            try {

                if (s.Network != null) {

                    var net = new TcpIpConnectionConfiguration();
                    b2 = net.CopyFrom(s.Network, out string? error);
                    if (b2) { Network = net; }
                }
                return b1 && b2;
            }
            catch {

                return false;
            }
        }

        public  bool CopyFrom(object src) {
            var s = src as IInterfaceConfiguration;

            return (s is null) ? false : CopyFrom(s);
        }

        public void Reset() {
            Network = null;
        }
    }
}
