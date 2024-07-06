using Grumpy.Common;
using Grumpy.Common.Utilities;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using System;
using System.IO.Ports;


namespace Grumpy.HWControl.Common.Handlers
{


    [JsonObject(MemberSerialization.OptIn)]
    public class SerialPortConfiguration: ConfigurationBase, IConfigurationBase
    {
        protected const int DefaultBaudRate = 9600;
        protected const int DefaultBits = 8;
        protected const Parity DefaultParity = Parity.None;
        protected const Handshake DefaultHandShake = Handshake.None;
        protected const StopBits DefaultStopBits = StopBits.One;
        protected const int DefaultReadWriteTimeoutMs = 200;
        protected const string _DefaultPortName = "NotSet";
        protected const int DefaultDelayBetweenTransactions = 100;


        private string? _portName;
        private int _baudRate;
        private Parity _parity;
        private Handshake _handShake;

        private StopBits _stopBits;
        private int _bits;

        private int _writeTimeoutMs;
        private int _readTimeoutMs;
        private int _minTimeBetweenTransactionsMs;

        private string? _txTerminator;
        private string? _rxTerminator;

        public SerialPortConfiguration() : base()
        {
            
        }

        public SerialPortConfiguration(string portName) : this()
        {
            _portName = portName;
        }

        public SerialPortConfiguration(SerialPortConfiguration source, string? newPortName = null) : this()
        {
            CopyFrom(source);

            if (newPortName != null) {
                _portName = newPortName;
            }
        }

        public override bool CopyFrom( object src )
        {

            var s = src as  SerialPortConfiguration;
            if (s == null) {
                LastErrorComment = "Source type is not compatible with SerialPortConfiguration type";
                return false;   
            }

            try {

                _portName = (string)(s.Name?.Clone() ??  null!);
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

                LastErrorComment = string.Empty;
                return true;
            }
            catch (Exception ex) {
                LastErrorComment = ex.Message;
                return false;
            }
        }

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
        }

        [JsonProperty]
        public string Name {
            get { return _portName == null ? string.Empty: _portName; }
            set { _portName = (string)value?.Clone()! ?? _DefaultPortName!; }
        }

        public bool PortNameIsDefault => 
            _portName?.Equals(_DefaultPortName, StringComparison.OrdinalIgnoreCase)?? true;

        [JsonProperty]
        public int ReadTimeoutMs {
            get { return _readTimeoutMs; }
            set { _readTimeoutMs = value; }
        }

        [JsonProperty]
        public int WriteTimeoutMs {
            get { return _writeTimeoutMs; }
            set { _writeTimeoutMs = value; }
        }


        [JsonProperty]
        public int MinTimeBetweenTransactionsMs {
            get { return _minTimeBetweenTransactionsMs; }
            set { _minTimeBetweenTransactionsMs = value; }
        }

        [JsonProperty]
        public string TxMessageTerminator {
            get =>_txTerminator == null ? string.Empty : _txTerminator;
            set => _txTerminator = (string)value?.Clone()! ?? null!; 
        }

        [JsonProperty]
        public string RxMessageTerminator {
            get => _rxTerminator == null ? string.Empty : _rxTerminator;
            set => _rxTerminator = (string)value?.Clone()! ?? null!; 
        }

        [JsonProperty]
        public int BaudRate {
            get { return _baudRate; }
            set { _baudRate = value; }
        }

        [JsonProperty]
        public int Bits {
            get { return _bits; }
            set { _bits = value; }
        }

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public Handshake HandShake {
            get { return _handShake; }
            set { _handShake = value; }
        }

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public StopBits StopBits {
            get { return _stopBits; }
            set { _stopBits = value; }
        }

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public Parity Parity {
            get { return _parity; }
            set { _parity = value; }
        }

        [JsonIgnore]
        public int WriteReadTimeout => WriteTimeoutMs + ReadTimeoutMs;

        public override string ToString()
        {
            try {
                string ret =  JsonConvert.SerializeObject(this,
                    Formatting.Indented);
                return ret;
            }
            catch (Exception e) {

                LastErrorComment = $"Failed to serialize object {this.GetType().Name}. " +
                    $"Exception {e.Message}";
                return string.Empty;
            }
        }
    }
}
