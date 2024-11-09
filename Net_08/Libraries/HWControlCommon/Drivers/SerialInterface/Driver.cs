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

using Grumpy.DAQFramework.Common;
using Grumpy.DAQFramework.Configuration;

using Serilog;

using Prts = System.IO.Ports;

namespace Grumpy.DAQFramework.Drivers.SerialInterface
{
    public class SeriaInterfaceDriver : ISerialInterface {

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
        private IOResults _IOResult;
        private SerialPortConfiguration? _portConfig;
        private PortState _state;
        private Prts.SerialPort? _serialPort;
        private int _connectRetryCounter;

        private object _lock;
        private object _stateLock;

        private ErrorHistory? _errorStack;


        public SeriaInterfaceDriver() {

            _lock = new object();
            _stateLock = new object();
            _connectRetryCounter = 0;
            _serialPort = null;
            _portConfig = null;
            State = PortState.Loaded;
            _errorStack = ErrorHistory.Create();
            LastIOResult = IOResults.NA;
        }

        object IOResultLock = new object();
        public IOResults LastIOResult {
            get {
                lock (IOResultLock) {
                    return _IOResult;
                }
            }
            private set {
                lock(IOResultLock) {
                    _IOResult = value;
                }
            }
        }
        public bool PortIsOpen => _serialPort?.IsOpen ?? false;

        public bool IsOpen => State == PortState.Connected;

        public bool InErrorState => State == PortState.Error;

        public PortState State {

            get {
                lock (_stateLock) {
                    return _state;
                }
            }
            private set {
                lock (_stateLock) {
                    _state = value;
                }
            }
        }

        public bool ConfigurationIsSet => _portConfig != null;

        public int BytesToRead => PortIsOpen ? 
                        _serialPort?.BytesToRead ?? 0 : 0;

        public bool HasBytesToRead => BytesToRead > 0;

        public bool SetPortConfiguration(SerialPortConfiguration config) {
            bool result = false;

            switch (State) {
                case (PortState.Connected):
                    _logger.Warning($"Serial Interface Driver, " +
                        $"SetConfiguration: port is connected. " +
                        $"Changes will take effect on next connection.");
                    State = PortState.Configured;
                    result = true;
                    break;

                case (PortState.Loaded):
                case (PortState.Configured):
                    _portConfig = config;
                    State = PortState.Configured;
                    result = true;
                    break;

                case (PortState.Error):
                    if (PortIsOpen) {
                        Close();
                        _logger.Information($"Serial Interface Driver, " +
                            $"SetConfiguration: Port is in error state " +
                            $"and connected. Disconnected " +
                            $"and configurateion set.");
                    }

                    State = PortState.Configured;
                    result = true;
                    break;

                default:
                    State = (_serialPort?.IsOpen ?? false)
                        ? PortState.Connected : PortState.Configured;
                    result = true;
                    break;
            }
            return result;
        }

        public bool Open() 
        {
            lock (_lock) {

                switch (State) {

                    case (PortState.Connected):
                    
                        _logger.Warning($"Serial Interface Driver Connect():" +
                            $" port is already connected. Request ignored.");
                        LastIOResult = IOResults.Ignored;
                        break;

                    case (PortState.Loaded):
                    case (PortState.Unknown):
                        
                        string err = $"Serial Interface Driver Connect():" +
                            $" port is not configured. Request ignored.";
                        _logger.Error(err);
                        _errorStack?.Push(
                            LogRecord.CreateErrorRecord(nameof(Open), err));

                        LastIOResult = IOResults.NotReady;
                        break;

                    case (PortState.Configured):

                        LastIOResult = _OpenPort() ? IOResults.Success : IOResults.Error;
                        break;

                    case (PortState.Error):
                        
                        _Disconnect();
                        State = PortState.Configured;
                        LastIOResult = _OpenPort() ? IOResults.Success : IOResults.Error;
                        break;

                    default:
                        _logger.Warning($"Serial Interface Driver " +
                            $"Connect(): Operation is not supported " +
                            $"in this state: {State.ToString()}.");
                        LastIOResult = IOResults.NotReady;
                        break;
                }
                return (LastIOResult & (IOResults.Success | IOResults.Ignored)) != 0;
            }
        }

        private bool _OpenPort() {
            string err;
            if ((_serialPort != null) && (_serialPort.IsOpen)) {

                return true;
            }

            if (string.IsNullOrEmpty(_portConfig?.Name) ||
                 _portConfig.PortNameIsDefault) {
                 err = $"Serial Interface Driver: Port not specified. " +
                    $"Check configuration file.";
                _logger?.Error(err);
                _errorStack?.Push(
                    LogRecord.CreateErrorRecord(nameof(_OpenPort), err));
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

                _serialPort.DataReceived += 
                    new Prts.SerialDataReceivedEventHandler(_DataReceivedHandler);
            }
            catch (Exception ex) {

                err = $"Serial Interface Driver OpenPort(). " +
                    $"Failed to configure " +
                    $"port. Exception: {ex.Message}" +
                     $" Configuration cleared.";
                _logger.Error(err);
                _errorStack?.Push(
                    LogRecord.CreateErrorRecord(nameof(_OpenPort), err));
                _portConfig = null;
                State = PortState.Loaded;
                return false;
            }

            _connectRetryCounter = DefaultMaxRetriesToOpen;

            while (_connectRetryCounter >= 0) {
                try {
                    _connectRetryCounter--;
                    _serialPort.Open();

                    if (_serialPort.IsOpen) {
                        State = PortState.Connected;
                        _LastTransactionTime = DateTime.Now;
                        return true;
                    }
                    else {
                        State = PortState.Configured;
                        return false;
                    }
                }
                catch (ArgumentException ex) {
                    err = $"Serial Interface Driver. OpenPort(). " +
                        $"Invalid port parameter. Exception: {ex.Message}";
                    _logger.Error(err);
                    _errorStack?.Push(
                        LogRecord.CreateErrorRecord(nameof(_OpenPort), err));
                    State = PortState.Error;
                    return false;
                }
                catch (IOException ex) {
                    err = $"Serial Interface Driver. OpenPort(). " +
                        $"Port is not available. Exception: {ex.Message}";
                    _logger.Error(err);
                    _errorStack?.Push(
                        LogRecord.CreateErrorRecord(nameof(_OpenPort), err));
                    State = PortState.Error;
                    return false;
                }
                catch (InvalidOperationException ex) {
                    err = $"Serial Interface Driver. OpenPort(). " +
                        $"Port is already open, probably by another process. " +
                        $"{ex.Message}. Trying again.";
                    _logger.Warning(err);
                    _errorStack?.Push(
                        LogRecord.CreateErrorRecord(nameof(_OpenPort), err));
                }
            }
            State = PortState.Error;
            err = $"Serial Interface Driver. Failed to open port within max " +
                $"number of attmpts; {DefaultMaxRetriesToOpen};";
 
            _errorStack?.Push(
                LogRecord.CreateErrorRecord(nameof(_OpenPort), err));
            throw (new IOException(err));
        }

        //public virtual IOResult Close() => Disconnect() ? IOResult.Success : IOResult.Error;

        public bool Close() {

            lock (_lock) {

                return _Disconnect();
            }
        }


        public bool Reset() {

            lock (_lock) {
                
                return (IsOpen && _Disconnect()) ? _OpenPort() : false;
            }
        }

        private bool _Disconnect() {

            if(_serialPort == null) {
                LastIOResult = IOResults.Ignored;
                return true;
            }

            if (PortIsOpen) {

                try {

                    _serialPort.Close();
                    _serialPort.Dispose();
                    LastIOResult = IOResults.Success;
                    State = PortState.Configured;
                    _serialPort = null;
                    return true;
                }
                catch (IOException ex) {

                    var err = $"Serial Interface Driver. Disconnect: " +
                        $"Failed to close port. Exception: {ex.Message}";
                    _logger.Error(err);
                    _errorStack?.Push(
                        LogRecord.CreateErrorRecord(
                                            nameof(_Disconnect), err));
                    State = PortState.Error;
                    LastIOResult = IOResults.Error;
                    return false;
                }
            }
            _logger.Warning($"Serial Interface Driver. Disconnect: " +
                $"Port is not open, request ignored.");

            if (_portConfig == null) {

                State = PortState.Loaded;
                LastIOResult = IOResults.NotReady;
                return false;
            }
            else {
                switch (State) {

                    case (PortState.Connected):
                    case (PortState.Error):
                    case (PortState.Configured):
                    case (PortState.Unknown):

                        State = (_portConfig == null) ? 
                                PortState.Loaded : PortState.Configured;
                        LastIOResult = IOResults.NotReady;
                        return true;

                    default:

                        var err = $"Serial Interface Driver Disconnect(): " +
                            $"Operation is not supported in " +
                            $"this state: {State.ToString()}.";
                        _errorStack?.Push(
                            LogRecord.CreateErrorRecord(
                                        nameof(_Disconnect), err));
                        _logger.Error(err);
                        LastIOResult = IOResults.NotReady;
                        return false;
  
                }
            }
        }

        public void ClearError() {

            lock (_lock) {

                _errorStack?.Clean();
            }
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

            if(_serialPort == null) {

                errorMsg = $"Serial Interface Driver. SendMessage: " +
                    $"Port is not open. Request ignored. ";
                _logger.Warning(errorMsg);
                _errorStack?.Push(
                    LogRecord.CreateErrorRecord(nameof(_Write), errorMsg));
                return false;
            }

            switch (State) {
                case (PortState.Error): {

                        errorMsg = $"Serial Interface Driver. SendMessage: " +
                        $"Handler is in error state. Request ingoned. ";
                        _logger.Warning(errorMsg);
                        _errorStack?.Push(
                            LogRecord.CreateErrorRecord(
                                                nameof(_Write), errorMsg));
                        result = false;
                    }
                    break;

                case (PortState.Connected): {
                        if (messageToSend == null) {
                            _logger.Warning($"Serial Interface Driver. " +
                                $"SendMessage: Message is null, " +
                                $"sending empty message.");
                            messageToSend = String.Empty;
                        }
                        else {
                            try {
                                _serialPort.Write(messageToSend + 
                                    _portConfig?.TxMessageTerminator ?? "");
                                result = true;
                            }
                            catch (InvalidOperationException ex) {
                                var err = $"Serial Interface Driver. " +
                                    $"SendMessage: Failed. " +
                                    $"Exception {ex.Message}";
                                _logger.Error(err);
                                _errorStack?.Push(
                                    LogRecord.CreateErrorRecord(
                                                    nameof(_Write), err));
                                State = PortState.Error;
                                result = false;
                            }
                            catch (TimeoutException ex) {
                                var err = $"Serial Interface Driver. " +
                                    $"SendMessage: Failed. Timeout. " +
                                    $"Exception {ex.Message}";
                                _logger.Error(err);
                                _errorStack?.Push(
                                    LogRecord.CreateErrorRecord(
                                                    nameof(_Write), err));
                                result = false;
                            }
                        }
                    }
                    break;

                default: {
                        _logger.Warning($"Serial Interface Driver. " +
                            $"SendMessage: Invalid port status: " +
                            $"{State.ToString()}.  Request ignored.");
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

            double timeToWait = _portConfig!.MinTimeBetweenTransactionsMs -
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
            string? errorMsg = null;

            if (State == PortState.Error) {
                errorMsg = $"Serial port driver. Receive: Handler " +
                    $"is in error state. Request ignored. ";
                _logger.Warning(errorMsg);
                _errorStack?.Push(
                    LogRecord.CreateErrorRecord(
                                    nameof(_Read), errorMsg));
                reply = (string)errorMsg.Clone();
                return PortInErrorState;
            }

            byte[] buffer = new byte[maxLen + 1];
            int bytesReceived = 0;

            if (State == PortState.Connected) {
                try {
                    
                    if (!string.IsNullOrEmpty(
                                _portConfig?.RxMessageTerminator ?? 
                                string.Empty)) {

                        string msg = _serialPort?.ReadTo(
                            _portConfig!.RxMessageTerminator) ??
                                    string.Empty;

                        reply = msg.Replace(
                                    _portConfig!.TxMessageTerminator, "");

                        _LastTransactionTime = DateTime.Now;
                        return reply.Length;

                    }
                    else {
                        DateTime timout = DateTime.Now + 
                            TimeSpan.FromMilliseconds(
                                            _portConfig!.ReadTimeoutMs);

                        bytesReceived = _serialPort!.Read(buffer, 0, maxLen);
                        // If we made it here, then we got some bytes.
                        // Try to get some more within timeout... .
                        int cntr = 0;
                        reply = String.Empty;

                        while ((DateTime.Now <= timout) && 
                                            (bytesReceived < minLen)) {

                            if (_serialPort.BytesToRead > 0) {
                                int bytesToRead = 
                                    Math.Min(maxLen - bytesReceived, 
                                                        _serialPort.BytesToRead);

                                if (bytesToRead > 0) {
                                    bytesReceived += 
                                        _serialPort.Read(buffer, bytesReceived, 
                                                                   bytesToRead);
                                }
                            }
                            else {
                                Thread.Sleep(20);
                                cntr++;
                            }
                        }
                        reply = System.Text.Encoding.UTF8.GetString(
                                                    buffer, 0, bytesReceived);
                        _LastTransactionTime = DateTime.Now;
                        return reply.Length;
                    }
                }
                catch (InvalidOperationException ex) {
                    errorMsg = $"Serial port driver. " +
                        $"Receive: Failed. Exception: ";
                    _logger.Error(errorMsg + ex.Message);
                    _errorStack?.Push(
                        LogRecord.CreateErrorRecord(
                            nameof(_Read), errorMsg + ex.Message));
                    State = PortState.Error;
                    return InvalidOperationError;
                }

                catch (TimeoutException ex) {

                    if (string.IsNullOrEmpty(reply)) {
                        reply = string.Empty;
                        _LastTransactionTime = DateTime.Now;
                        return TimeoutError;
                    }

                    if (!string.IsNullOrEmpty(_portConfig?.RxMessageTerminator)) {
                        errorMsg = $"Serial port driver. Receive: Timeout. ";
                        _logger.Warning(errorMsg + ex.Message);
                        _errorStack?.Push(
                            LogRecord.CreateErrorRecord(
                                nameof(_Read), errorMsg + ex.Message));
                        State = PortState.Error;
                        return TimeoutError;
                    }
                    else {
                        _LastTransactionTime = DateTime.Now;
                        return reply.Length;
                    }
                }

                catch (ArgumentException ex) {
                    reply = string.Empty;
                    errorMsg = $"Serial port driver. " +
                        $"Receive: Terminator is " +
                        $"null or empty. Request ignored. ";
                    _logger.Error(errorMsg + ex.Message);
                    _errorStack?.Push(
                        LogRecord.CreateErrorRecord(
                            nameof(_Read), errorMsg + ex.Message));
                    State = PortState.Error;
                    return TerminatorMissingError;

                }
            }
            _logger.Warning($"Serial port driver. Receive: " +
                $"Port is not connected. Status: " +
                $"{State.ToString()}. Request ignored.");
            _LastTransactionTime = DateTime.Now;
            return 0;

        }


        public bool FlushRxBuffer() {
            lock (_lock) {
                return _FlushRxBuffer();
            }
        }

        private bool _FlushRxBuffer() {
            if (PortIsOpen) {

                if(_serialPort == null) {

                    string err = "Serial port driver. FlushRxBuffer: " +
                        "Port is not open. Request ignored.";
                    _errorStack?.Push(
                        LogRecord.CreateErrorRecord(
                            nameof(_FlushRxBuffer), err));
                    _logger.Error(err);
                    return false;
                }

                try {
                    _serialPort?.DiscardInBuffer() ;
                    return true;
                }
                catch (InvalidOperationException e) {
                    var err = $"Serial port driver. FlushRxBuffer: " +
                        $"Invalid port setting(s). Exception {e.Message}";
                    _errorStack?.Push(
                        LogRecord.CreateErrorRecord(
                            nameof(_FlushRxBuffer), err));
                    _logger.Error(err);
                    State = PortState.Error;
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
            if (PortIsOpen) {

                if (_serialPort == null) {

                    string err = "Serial port driver. FlushTxBuffer: " +
                        "Port is not open. Request ignored.";
                    _errorStack?.Push(
                        LogRecord.CreateErrorRecord(
                                    nameof(_FlushTxBuffer), err));
                    _logger.Error(err);
                    return false;
                }

                try {

                    _serialPort.DiscardOutBuffer();
                    return true;
                }
                catch (InvalidOperationException e) {

                    var err = $"Serial port driver. " +
                        $"FlushTxBuffer: Invalid port setting(s). " +
                        $"Exception {e.Message}";
                    _errorStack?.Push(
                        LogRecord.CreateErrorRecord(nameof(_FlushTxBuffer), err));
                    _logger.Error(err);
                    State = PortState.Error;
                    return false;
                }
            }

            return true;
        }

        public bool FlushBuffers() => (FlushRxBuffer()) && (FlushTxBuffer());


        public int Query(string message, out string response, 
            int maxLen = UseDefault, int minLen = UseDefault) {
            
            lock (_lock) {

                return _Query(message, out response, maxLen, minLen);
            }
        }


        private int _Query(string message, out string response, 
            int maxLen = UseDefault, int minLen = UseDefault) {
            response = String.Empty;
            
            if (!PortIsOpen) {

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


        public virtual bool Command(string cmd, string conformation, 
                                            int timeoutMs = 200) {

            lock (_lock) {

                return _Command(cmd, conformation, timeoutMs);
            }

        }


        private bool _Command(string cmd, string conformation, 
                                            int timeoutMs = -1) {

            if (timeoutMs < 1) {

                timeoutMs = _portConfig?.WriteReadTimeout ?? 
                    SerialPortConfiguration.DefaultReadWriteTimeoutMs;
            }

            DateTime timeout =
                    DateTime.Now.AddMilliseconds(timeoutMs);


            while (DateTime.Now < timeout) {

                int result = _Query(cmd, out string response);
                if (result > 0) {

                    if (response.Contains(conformation)) {

                        return true;
                    }
                    else {

                        var err = $"Serial port driver. " +
                            $"Command {cmd} error. " +
                            $"Unexpected response {response}.";
                        _logger.Error(err);
                        _errorStack?.Push(
                            LogRecord.CreateErrorRecord(nameof(_Command), err));
                        return false;
                    }
                }
                else if (result == 0) {

                    if (String.IsNullOrEmpty(response)) {

                        return true;
                    }
                    else {

                        var err = $"Serial port driver. " + 
                            $"Command {cmd} error. " + 
                            $"Unexpected response {response}.";
                        _logger.Error(err);
                        _errorStack?.Push(
                            LogRecord.CreateErrorRecord(nameof(_Command), err));
                        return false;
                    }
                }

                else if (result < 0) {
                    var err = $"Serial port driver. " +
                        $"Command {cmd} error. " +
                        $"{(String.IsNullOrEmpty(response) ? "" : response)}";
                    _logger.Error(err);
                    _errorStack?.Push(
                        LogRecord.CreateErrorRecord(nameof(_Command), err));
                    return false;
                }
                Thread.Sleep(30);
            }

            _logger.Error($"Serial port driver. " +
                $"Command {cmd} timeout.");
            return false;
        }

        public string LastError {

            get {
                if( _errorStack == null || _errorStack.Count < 1) {
                    return string.Empty;
                }
                _errorStack.Peek( out LogRecord? err);

                return err?.Details ?? string.Empty;
            }

        }



        private static void _DataReceivedHandler(
                     object sender,
                     Prts.SerialDataReceivedEventArgs e) {

            Prts.SerialPort sp = (Prts.SerialPort)sender;
            string indata = sp.ReadExisting();
            Console.WriteLine("Data Received:");
            Console.Write(indata);
        }
    }
}
