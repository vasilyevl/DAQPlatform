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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Grumpy.SDAQFramework.Configuration
{
    /// <summary>
    /// Defines the contract for interface configuration, supporting both network and serial port settings.
    /// </summary>
    public interface IInterfaceConfiguration
    {
        /// <summary>
        /// Gets or sets the currently active interface type.
        /// </summary>
        InterfaceSelector ActiveInterface { get; set; }

        /// <summary>
        /// Gets or sets the network (TCP/IP) connection configuration.
        /// </summary>
        TcpIpConnectionConfiguration? Network { get; set; }

        /// <summary>
        /// Gets or sets the serial port configuration.
        /// </summary>
        SerialPortConfiguration? SerialPort { get; set; }
    }

    /// <summary>
    /// Represents a configuration for a communication interface, supporting both network and serial port options.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class InterfaceConfiguration : ConfigurationBase, IInterfaceConfiguration
    {
        private const InterfaceSelector _DefaultInterface = InterfaceSelector.Auto;

        private TcpIpConnectionConfiguration? _network;
        private SerialPortConfiguration? _serialPort;
        private InterfaceSelector _activeInterface;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceConfiguration"/> class with default values.
        /// </summary>
        public InterfaceConfiguration() : base()
        {
            ActiveInterface = _DefaultInterface;
            Network = null;
            SerialPort = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceConfiguration"/> class by copying from another configuration.
        /// </summary>
        /// <param name="src">The source configuration to copy from.</param>
        public InterfaceConfiguration(IInterfaceConfiguration src) : this()
        {
            Network = (src?.Network is not null) ?
                (src.Network.Clone(out string? error) as TcpIpConnectionConfiguration) :
                null;
        }

        /// <summary>
        /// Resets the configuration to its default state.
        /// </summary>
        public override void Reset()
        {
            Network = null;
            SerialPort = null;
            ActiveInterface = _DefaultInterface;
        }

        /// <summary>
        /// Copies configuration values from another object implementing <see cref="IInterfaceConfiguration"/>.
        /// </summary>
        /// <param name="src">The source object to copy from.</param>
        /// <returns>True if the copy was successful; otherwise, false.</returns>
        public override bool CopyFrom(object src)
        {
            if (src is IInterfaceConfiguration config) {
                ActiveInterface = config.ActiveInterface;
                Network = config.Network?.Clone(out string? error) as TcpIpConnectionConfiguration;
                SerialPort = config.SerialPort?.Clone(out error) as SerialPortConfiguration;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets or sets the currently active interface type.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public InterfaceSelector ActiveInterface
        {
            get => _activeInterface;
            set => _activeInterface = value;
        }

        /// <summary>
        /// Gets or sets the network (TCP/IP) connection configuration.
        /// </summary>
        [JsonProperty]
        public TcpIpConnectionConfiguration? Network
        {
            get => _network;
            set => _network = value;
        }

        /// <summary>
        /// Gets or sets the serial port configuration.
        /// </summary>
        [JsonProperty]
        public SerialPortConfiguration? SerialPort
        {
            get => _serialPort;
            set => _serialPort = value;
        }

        /// <summary>
        /// Copies network configuration from another <see cref="IInterfaceConfiguration"/> instance.
        /// </summary>
        /// <param name="s">The source configuration.</param>
        /// <returns>True if the copy was successful; otherwise, false.</returns>
        internal bool CopyFrom(IInterfaceConfiguration s)
        {
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
    }
}
