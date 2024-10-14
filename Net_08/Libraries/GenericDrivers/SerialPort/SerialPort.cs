using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using Grumpy.HWControl.Configuration;
using Serilog;

namespace Grumpy.Drivers.SerialPort
{
    public class SerialPort
    {
        public enum PortState
        {
            Unknown,
            Error,
            Loaded,
            Configured,
            Connected,
        }

        public interface ISerialInterface
        {
            bool SetPortConfiguration(SerialPortConfiguration config);

            bool Open();

            bool Close();

            bool IsConnected { get; }

            bool Write(string message);

            int Read(out string message, int maxLen, int minLen);

            int Query(string message, out string response, int maxLen, int minLen);

            // Serial Port Only 
            bool FlushRxBuffer();

            bool FlushTxBuffer();

            bool FlushBuffers();

            PortState Status { get; }

            bool PortIsOpen { get; }

            bool InErrorState { get; }

            bool HasBytesToRead { get; }

            int BytesToRead { get; }

        }

        public class SeriaInterfaceHandler : ISerialInterface
        {
            protected static ILogger _logger = Log.Logger;

            protected const int DefaultMaxRetriesToOpen = 5;
            protected const int DefaultMaxReplyMessagelength = 256;
            public const int UseDefault = -1;

            public const int PortIsNotOpen = -1;
            public const int PortInErrorState = -2;
            public const int InvalidOperationError = -3;
            protected const int TimeoutError = -4;
            protected const int TerminatorMissingError = -5;
            protected const int RxTxFlushError = -6;
            protected const int SendError = -7;

            private SerialPortConfiguration _portConfig;
            private PortState _state;
            private Prts.SerialPort _serialPort;
            private int _connectRetryCounter;

            private object _lock = new object();

            public SeriaInterfaceHandler() {
                _connectRetryCounter = 0;
                _serialPort = null;
                _portConfig = null;
                _state = PortState.Loaded;
            }

            public bool PortIsOpen => _serialPort.IsOpen;

            public bool IsConnected => _state == PortState.Connected;

            public bool InErrorState => _state == PortState.Error;

            public PortState Status => _state;

            public bool ConfigurationIsSet => _portConfig != null;

            public int BytesToRead => (PortIsOpen) ? _serialPort.BytesToRead : 0;

            public bool HasBytesToRead => BytesToRead > 0;

            public bool SetPortConfiguration(SerialPortConfiguration config) {
                bool result = false;
                _portConfig = config;

                switch (_state) {
                    case (PortState.Connected):
                        _logger.Warning($"SeriaIfHandler, SetConfiguration: " +
                            $"port is connected. changes will take effect on next connection.");
                        result = true;
                        break;

                    case (PortState.Loaded):
                    case (PortState.Configured):
                        _state = PortState.Configured;
                        result = true;
                        break;

                    case (PortState.Error):
                        if (_serialPort.IsOpen) {
                            Close();
                            _logger.Information($"SeriaIfHandler, SetConfiguration: " +
                                $"Port was in error state and connected. Disconnected and configurateion set.");
                        }
                        _state = PortState.Configured;
                        result = true;
                        break;

                    default:
                        _state = (_serialPort.IsOpen) ? PortState.Connected : PortState.Configured;
                        result = true;
                        break;
                }
                return result;
            }

            //public virtual IOResult Open(string signature = null) => Connect() ? IOResult.Success : IOResult.Error;

            public IOState Open() {
                lock (_lock) {

                    IOState result = IOState.Error;

                    switch (_state) {
                        case (PortState.Connected):
                            _logger.Warning($"SeriaIfHandler Connect(): port is " +
                                $"already connected. Request ignored.");
                            result = IOState.Success;
                            break;

                        case (PortState.Loaded):
                        case (PortState.Unknown):
                            _logger.Error($"SeriaIfHandler Connect(): port is not " +
                                $"configured. Request ignored.");
                            result = IOState.Success;
                            break;

                        case (PortState.Configured):
                            result = _OpenPort() ? IOState.Success : IOState.Error;
                            break;

                        case (PortState.Error):
                            _Disconnect();
                            _state = PortState.Configured;
                            result = _OpenPort() ? IOState.Success : IOState.Error;
                            break;

                        default:
                            _logger.Warning($"SeriaIfHandler Connect(): Operation is " +
                                $"not supported in this state: {_state.ToString()}.");
                            result = IOState.Error;
                            break;
                    }
                    return result;
                }
            }

            private bool _OpenPort() {
                if ((_serialPort != null) && (_serialPort.IsOpen)) {

                    return true;
                }

                if (string.IsNullOrEmpty(_portConfig?.Name ?? null) ||
                     _portConfig.PortNameIsDefault) {

                    _logger.Error($"Serial port handler: Port not specified. " +
                        $"Check configuration file.");
                    return false;
                }
                _serialPort = new Prts.SerialPort();
                try {
                    _serialPort.PortName = _portConfig.Name;
                    _serialPort.BaudRate = _portConfig.BaudRate;
                    _serialPort.Parity = _portConfig.Parity;
                    _serialPort.DataBits = _portConfig.Bits;
                    _serialPort.StopBits = _portConfig.StopBits;
                    _serialPort.ReadTimeout = _portConfig.ReadTimeoutMs;
                    _serialPort.WriteTimeout = _portConfig.WriteTimeoutMs;
                    _serialPort.Handshake = _portConfig.HandShake;
                    if (_portConfig.HandShake == Prts.Handshake.None) {
                        _serialPort.DtrEnable = true;
                        _serialPort.RtsEnable = true;
                    }
                }
                catch (Exception ex) {
                    _logger.Error($"SerialPort. OpenPort(). Failed to configure " +
                        $"port. Exception: {ex.Message}" +
                         $" Configuration cleared.");
                    _portConfig = null;
                    _state = PortState.Loaded;
                    return false;
                }

                _connectRetryCounter = DefaultMaxRetriesToOpen;

                while (_connectRetryCounter >= 0) {
                    try {
                        _connectRetryCounter--;
                        _serialPort.Open();

                        if (_serialPort.IsOpen) {
                            _state = PortState.Connected;
                            _LastTransactionTime = DateTime.Now;
                            return true;
                        }
                        else {
                            _state = PortState.Configured;
                            return false;
                        }
                    }
                    catch (ArgumentException ex) {
                        _logger.Error($"SerialPort. OpenPort(). Invalid port " +
                            $"paraqmeter. Exception: {ex.Message}");
                        _state = PortState.Error;
                        return false;
                    }
                    catch (IOException ex) {
                        _logger.Error($"SerialPort. OpenPort(). Port state or " +
                            $"parameters are invalid. Exception: {ex.Message}");
                        _state = PortState.Error;
                        return false;
                    }
                    catch (InvalidOperationException ex) {
                        _logger.Warning($"SerialPort. OpenPort(). Port is " +
                            $"already open, probably by another process. " +
                            $"{ex.Message}. Trying again.");
                    }
                }
                _state = PortState.Error;
                throw (new IOException($"Failed to open port within max " +
                    $"number of attmpts; {DefaultMaxRetriesToOpen};"));
            }

            //public virtual IOResult Close() => Disconnect() ? IOResult.Success : IOResult.Error;

            public IOState Close() {
                lock (_lock) {
                    return _Disconnect();
                }
            }

            private IOState _Disconnect() {
                IOState result = IOState.Error;

                if (_serialPort.IsOpen) {
                    try {
                        _serialPort.Close();
                        result = IOState.Success;
                        _state = PortState.Configured;
                    }
                    catch (IOException ex) {
                        _logger.Error($"Serial port handler.Disconnect: " +
                            $"Failed to close port.Exception: {ex.Message}");
                        _state = PortState.Error;
                        result = IOState.Error;
                    }

                    return result;
                }
                _logger.Warning($"Serial port handler.Disconnect: " +
                    $"Port is not open, request ignored.");

                // Poer is not connected. Veryfy if status makes sense:

                if (_portConfig == null) {
                    _state = PortState.Loaded;
                    result = IOState.Success;
                }
                else {
                    switch (_state) {
                        case (PortState.Connected):
                        case (PortState.Error):
                        case (PortState.Configured):
                        case (PortState.Unknown):
                            _state = (_portConfig == null) ? PortState.Loaded : PortState.Configured;
                            result = IOState.Success;
                            break;
                        default:
                            _logger.Error($"SeriaIfHandler Disconnect(): " +
                                $"Operation is not supported in this state: {_state.ToString()}.");
                            result = IOState.Error;
                            break;
                    }
                }
                return result;
            }

            public bool Write(string message) {
                lock (_lock) {
                    return _Write(message);
                }
            }

            public DateTime _LastTransactionTime { get; private set; }
            private bool _Write(string messageToSend) {
                _Wait();
                string errorMsg = string.Empty;
                bool result = false;

                switch (_state) {
                    case (PortState.Error): {
                            errorMsg = $"Serial port handler. SendMessage: " +
                            $"Handler is in error state. Request ingoned. ";
                            _logger.Warning(errorMsg);
                            result = false;
                        }
                        break;

                    case (PortState.Connected): {
                            if (messageToSend == null) {
                                _logger.Warning($"Serial port handler. SendMessage: " +
                                    $"Message is null, sending empty message.");
                                messageToSend = String.Empty;
                            }
                            else {
                                try {
                                    _serialPort.Write(messageToSend + _portConfig.TxMessageTerminator);
                                    result = true;
                                }
                                catch (InvalidOperationException ex) {
                                    _logger.Error($"Serial port handler. SendMessage failed. " +
                                        $"Exception {ex.Message}");
                                    _state = PortState.Error;
                                    result = false;
                                }
                                catch (TimeoutException ex) {
                                    _logger.Error($"Serial port handler. " +
                                        $"SendMessage failed. Timeout. Exception {ex.Message}");
                                    _state = PortState.Error;
                                    result = false;
                                }
                            }
                        }
                        break;

                    default: {
                            _logger.Warning($"Serial port handler. " +
                                $"SendMessage: Invalid port status: " +
                                $"{_state.ToString()}.  Request ignored.");
                            result = false;
                        }
                        break;
                }
                _LastTransactionTime = DateTime.Now;
                return result;
            }

            public int Read(out string reply,
                int maxLen = UseDefault, int minLen = UseDefault) {
                lock (_lock) {
                    return _Read(out reply, maxLen, minLen);
                }
            }


            private void _Wait() {
                double timeToWait = _portConfig.MinTimeBetweenTransactionsMs -
                    (DateTime.Now - _LastTransactionTime).TotalMilliseconds;
                if (timeToWait > 0) {

                    if (timeToWait > 10) {
                        int tms = Convert.ToInt32(timeToWait);
                        Thread.Sleep(tms);
                    }
                    else {
                        SpinWait spinWait = new SpinWait();
                        while ((_portConfig.MinTimeBetweenTransactionsMs -
                            (DateTime.Now - _LastTransactionTime).TotalMilliseconds) > 0) {
                            spinWait.SpinOnce();
                        }
                    }
                }
            }


            private int _Read(out string reply,
                int maxLen = UseDefault, int minLen = UseDefault) {
                _Wait();
                if (maxLen < 0) { maxLen = DefaultMaxReplyMessagelength; }
                if (minLen < 0) { minLen = maxLen + 1; }

                reply = string.Empty;
                string errorMsg = null;

                if (_state == PortState.Error) {
                    errorMsg = $"Serial port handler. Receive: Handler " +
                        $"is in error state. Request ignored. ";
                    _logger.Warning(errorMsg);
                    reply = (string)errorMsg.Clone();
                    return PortInErrorState;
                }

                byte[] buffer = new byte[maxLen + 1];
                int bytesReceived = 0;

                if (_state == PortState.Connected) {
                    try {

                        if (!string.IsNullOrEmpty(_portConfig.RxMessageTerminator)) {

                            string msg = _serialPort.ReadTo(_portConfig.RxMessageTerminator);

                            reply = msg.Replace(_portConfig.TxMessageTerminator, "");

                            _LastTransactionTime = DateTime.Now;
                            return reply.Length;

                        }
                        else {
                            DateTime timout = DateTime.Now + TimeSpan.FromMilliseconds(_portConfig.ReadTimeoutMs);

                            bytesReceived = _serialPort.Read(buffer, 0, maxLen);
                            // If we made it here, then we got some bytes.
                            // Try to get some more within timeout... .
                            int cntr = 0;
                            reply = String.Empty;

                            while ((DateTime.Now <= timout) && (bytesReceived < minLen)) {

                                if (_serialPort.BytesToRead > 0) {
                                    int bytesToRead = Math.Min(maxLen - bytesReceived, _serialPort.BytesToRead);
                                    if (bytesToRead > 0) {
                                        bytesReceived += _serialPort.Read(buffer, bytesReceived, bytesToRead);
                                    }
                                }
                                else {
                                    Thread.Sleep(20);
                                    cntr++;
                                }
                            }
                            reply = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                            _LastTransactionTime = DateTime.Now;
                            return reply.Length;
                        }
                    }
                    catch (InvalidOperationException ex) {
                        errorMsg = $"Serial port handler. Receive: Failed. Exception: ";
                        _logger.Error(errorMsg + ex.Message);
                        _state = PortState.Error;
                        return InvalidOperationError;
                    }

                    catch (TimeoutException ex) {

                        if (string.IsNullOrEmpty(reply)) {
                            reply = string.Empty;
                            _LastTransactionTime = DateTime.Now;
                            return TimeoutError;
                        }

                        if (!string.IsNullOrEmpty(_portConfig.RxMessageTerminator)) {
                            errorMsg = $"Serial port handler. Receive: Timeout. ";
                            _logger.Warning(errorMsg + ex.Message);
                            _state = PortState.Error;
                            return TimeoutError;
                        }
                        else {
                            _LastTransactionTime = DateTime.Now;
                            return reply.Length;
                        }
                    }

                    catch (ArgumentException ex) {
                        reply = string.Empty;
                        errorMsg = $"Serial port handler. Receive: Terminator is " +
                            $"null or empty. Request ignored. ";
                        _logger.Error(errorMsg + ex.Message);
                        _state = PortState.Error;
                        return TerminatorMissingError;

                    }
                }
                _logger.Warning($"Serial port handler. Receive: " +
                    $"Port is not connected. Status: {_state.ToString()}. Request ignored.");
                _LastTransactionTime = DateTime.Now;
                return 0;

            }


            public bool FlushRxBuffer() {
                lock (_lock) {
                    return _FlushRxBuffer();
                }
            }

            private bool _FlushRxBuffer() {
                if (_serialPort.IsOpen) {
                    try {
                        _serialPort.DiscardInBuffer();
                        return true;
                    }
                    catch (InvalidOperationException e) {
                        _logger.Error($"Serial port handler.FlushRxBuffer: " +
                            $"Invalid port setting(s). Exception {e.Message}");
                        _state = PortState.Error;
                        return false;
                    }
                }
                return true;
            }

            public bool FlushTxBuffer() {
                lock (_lock) {
                    return _FlushTxBuffer();
                }
            }

            private bool _FlushTxBuffer() {
                if (_serialPort.IsOpen) {
                    try {
                        _serialPort.DiscardOutBuffer();
                        return true;
                    }
                    catch (InvalidOperationException e) {
                        _logger.Error($"Serial port handler.FlushTxBuffer: Invalid port setting(s)." +
                             $" Exception {e.Message}");
                        _state = PortState.Error;
                        return false;
                    }
                }
                return true;
            }

            public bool FlushBuffers() => (FlushRxBuffer()) && (FlushTxBuffer());


            public int Query(string message, out string response, int maxLen = UseDefault, int minLen = UseDefault) {
                lock (_lock) {
                    return _Query(message, out response, maxLen, minLen);
                }
            }


            private int _Query(string message, out string response, int maxLen = UseDefault, int minLen = UseDefault) {
                response = String.Empty;
                if (!_serialPort.IsOpen) {
                    return PortIsNotOpen;
                }

                if (!(_FlushRxBuffer() && _FlushTxBuffer())) {
                    response = "failed to flush";
                    return RxTxFlushError;
                }

                if (_Write(message)) {

                    return _Read(out response, maxLen, minLen);
                }

                response = "Failed to send.";
                return SendError;
            }


            public virtual IOState Command(string cmd, string conformation, int timeoutMs = 200) {
                lock (_lock) {
                    return _Command(cmd, conformation, timeoutMs);
                }

            }


            private IOState _Command(string cmd, string conformation, int timeoutMs = -1) {

                if (timeoutMs < 1) {
                    timeoutMs = _portConfig.WriteReadTimeout;
                }

                DateTime timeout =
                        DateTime.Now.AddMilliseconds(timeoutMs);


                while (DateTime.Now < timeout) {

                    int result = _Query(cmd, out string response);
                    if (result > 0) {
                        if (response.Contains(conformation)) {
                            return IOState.Success;
                        }
                        else {
                            _logger.Error($"Serial port handler. Command {cmd} error. " +
                                $"Unexpected response {response}.");
                            return IOState.Error;
                        }
                    }
                    else if (result == 0) {
                        if (String.IsNullOrEmpty(response)) {
                            return IOState.Success;
                        }
                        else {
                            _logger.Error($"Serial port handler. Command {cmd} error. " +
                                $"Unexpected response {response}.");
                            return IOState.Error;
                        }
                    }

                    else if (result < 0) {
                        _logger.Error($"Serial port handler. Command {cmd} error. " +
                            $"{(String.IsNullOrEmpty(response) ? "" : response)}");
                        return IOState.Error;
                    }
                    Thread.Sleep(30);
                }
                _logger.Error($"Serial port handler. Command {cmd} timeout.");
                return IOState.Error;
            }
        }
    }
}
