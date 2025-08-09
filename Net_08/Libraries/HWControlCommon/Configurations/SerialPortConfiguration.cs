/*
 
Copyright (c) 2024 vasilyevl (Grumpy). Permission is hereby granted, 
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

using System.IO.Ports;
using System.Runtime.CompilerServices;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Grumpy.SDAQFramework.Configuration
{
    /// <summary>
    /// Represents the configuration settings for a serial port connection, including port name, baud rate, parity, handshake, stop bits, timeouts, and message terminators.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class SerialPortConfiguration : ConfigurationBase, IConfigurationBase
    {
        /// <summary>Default baud rate (9600).</summary>
        public const int DefaultBaudRate = 9600;
        /// <summary>Default number of data bits (8).</summary>
        public const int DefaultBits = 8;
        /// <summary>Default parity (None).</summary>
        public const Parity DefaultParity = Parity.None;
        /// <summary>Default handshake (None).</summary>
        public const Handshake DefaultHandShake = Handshake.None;
        /// <summary>Default stop bits (None).</summary>
        public const StopBits DefaultStopBits = StopBits.None;
        /// <summary>Default read/write timeout in milliseconds (100 ms).</summary>
        public const int DefaultReadWriteTimeoutMs = 100;
        /// <summary>Default port name ("NotSet").</summary>
        public const string _DefaultPortName = "NotSet";
        /// <summary>Default delay between transactions in milliseconds (100 ms).</summary>
        public const int DefaultDelayBetweenTransactions = 100;
        /// <summary>Default connect timeout in milliseconds (1000 ms).</summary>
        public const int DeafultConnectTimeoutMs = 1000;
        /// <summary>Default message terminator ("\n").</summary>
        public const string DefaultTerminator = "\n";

        private string? _portName;
        private int _baudRate;
        private Parity _parity;
        private Handshake _handShake;
        private StopBits _stopBits;
        private int _bits;
        private int _writeTimeoutMs;
        private int _readTimeoutMs;
        private int _minTimeBetweenTransactionsMs;
        private int _connectTimeoutMs;
        private string? _txTerminator;
        private string? _rxTerminator;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortConfiguration"/> class with default values.
        /// </summary>
        public SerialPortConfiguration() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortConfiguration"/> class with the specified port name.
        /// </summary>
        /// <param name="portName">The serial port name.</param>
        public SerialPortConfiguration(string portName) : this()
        {
            _portName = portName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortConfiguration"/> class by copying from another instance, optionally overriding the port name.
        /// </summary>
        /// <param name="source">The source configuration to copy from.</param>
        /// <param name="newPortName">The new port name to use, or null to keep the source port name.</param>
        public SerialPortConfiguration(SerialPortConfiguration source, string? newPortName = null) : this()
        {
            CopyFrom(source);
            if (newPortName != null) {
                _portName = newPortName;
            }
        }

        /// <summary>
        /// Copies configuration values from another <see cref="SerialPortConfiguration"/> instance.
        /// </summary>
        /// <param name="src">The source object to copy from.</param>
        /// <returns>True if the copy was successful; otherwise, false.</returns>
        public override bool CopyFrom(object? src)
        {
            var s = src as SerialPortConfiguration;
            if (s == null) {
                LastError = "Source type is not compatible with SerialPortConfiguration type";
                return false;
            }

            try {
                _portName = (string) (s.Name?.Clone() ?? null!);
                _baudRate = s.BaudRate;
                _parity = s.Parity;
                _handShake = s.HandShake;
                _stopBits = s.StopBits;
                _bits = s._bits;
                _readTimeoutMs = s.ReadTimeoutMs;
                _writeTimeoutMs = s.WriteTimeoutMs;
                _txTerminator = s.TxMessageTerminator;
                _rxTerminator = s.RxMessageTerminator;
                _minTimeBetweenTransactionsMs = s.MinTimeBetweenTransactionsMs;
                LastError = string.Empty;
                return true;
            }
            catch (Exception ex) {
                LastError = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Resets the configuration to default values.
        /// </summary>
        public override void Reset()
        {
            _portName = _DefaultPortName;
            _baudRate = DefaultBaudRate;
            _parity = DefaultParity;
            _handShake = DefaultHandShake;
            _stopBits = DefaultStopBits;
            _bits = DefaultBits;
            _readTimeoutMs = DefaultReadWriteTimeoutMs;
            _writeTimeoutMs = DefaultReadWriteTimeoutMs;
            _txTerminator = null;
            _rxTerminator = null;
            _minTimeBetweenTransactionsMs = DefaultDelayBetweenTransactions;
            _connectTimeoutMs = DeafultConnectTimeoutMs;
        }

        /// <summary>
        /// Gets or sets the serial port name.
        /// </summary>
        [JsonProperty]
        public string Name
        {
            get { return _portName == null ? string.Empty : _portName; }
            set { _portName = (string) value?.Clone()! ?? _DefaultPortName!; }
        }

        /// <summary>
        /// Gets a value indicating whether the port name is set to the default value.
        /// </summary>
        public bool PortNameIsDefault =>
            _portName?.Equals(_DefaultPortName, StringComparison.OrdinalIgnoreCase) ?? true;

        /// <summary>
        /// Gets or sets the read timeout in milliseconds.
        /// </summary>
        [JsonProperty]
        public int ReadTimeoutMs
        {
            get { return _readTimeoutMs; }
            set { _readTimeoutMs = value; }
        }

        /// <summary>
        /// Gets or sets the write timeout in milliseconds.
        /// </summary>
        [JsonProperty]
        public int WriteTimeoutMs
        {
            get { return _writeTimeoutMs; }
            set { _writeTimeoutMs = value; }
        }

        /// <summary>
        /// Gets or sets the connect timeout in milliseconds.
        /// </summary>
        [JsonProperty]
        public int ConnectTimeoutMs
        {
            get { return _connectTimeoutMs; }
            set { _connectTimeoutMs = value; }
        }

        /// <summary>
        /// Gets or sets the minimum time between transactions in milliseconds.
        /// </summary>
        [JsonProperty]
        public int MinTimeBetweenTransactionsMs
        {
            get { return _minTimeBetweenTransactionsMs; }
            set { _minTimeBetweenTransactionsMs = value; }
        }

        /// <summary>
        /// Gets or sets the message terminator for transmitted messages.
        /// </summary>
        [JsonProperty]
        public string TxMessageTerminator
        {
            get => _txTerminator == null ? string.Empty : _txTerminator;
            set => _txTerminator = (string) value?.Clone()! ?? null!;
        }

        /// <summary>
        /// Gets or sets the message terminator for received messages.
        /// </summary>
        [JsonProperty]
        public string RxMessageTerminator
        {
            get => _rxTerminator == null ? string.Empty : _rxTerminator;
            set => _rxTerminator = (string) value?.Clone()! ?? null!;
        }

        /// <summary>
        /// Gets or sets the baud rate.
        /// </summary>
        [JsonProperty]
        public int BaudRate
        {
            get { return _baudRate; }
            set { _baudRate = value; }
        }

        /// <summary>
        /// Gets or sets the number of data bits.
        /// </summary>
        [JsonProperty]
        public int Bits
        {
            get { return _bits; }
            set { _bits = value; }
        }

        /// <summary>
        /// Gets or sets the handshake protocol.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public Handshake HandShake
        {
            get { return _handShake; }
            set { _handShake = value; }
        }

        /// <summary>
        /// Gets or sets the stop bits setting.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public StopBits StopBits
        {
            get { return _stopBits; }
            set { _stopBits = value; }
        }

        /// <summary>
        /// Gets or sets the parity setting.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public Parity Parity
        {
            get { return _parity; }
            set { _parity = value; }
        }

        /// <summary>
        /// Gets the total timeout for a write-read operation (sum of write and read timeouts).
        /// </summary>
        [JsonIgnore]
        public int WriteReadTimeout => WriteTimeoutMs + ReadTimeoutMs;

        /// <summary>
        /// Returns a JSON string representation of the configuration.
        /// </summary>
        /// <returns>A JSON string representing the configuration, or an empty string if serialization fails.</returns>
        public override string ToString()
        {
            try {
                string ret = JsonConvert.SerializeObject(this, Formatting.Indented);
                return ret;
            }
            catch (Exception e) {
                LastError = $"Failed to serialize object {this.GetType().Name}. Exception {e.Message}";
                return string.Empty;
            }
        }
    }
}
