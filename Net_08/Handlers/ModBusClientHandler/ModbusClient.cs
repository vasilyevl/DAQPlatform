/*
This code is based on  2018-2020 Rossmann-Engineering EasyModbus project. 
The original code is available at:
https://github.com/rossmann-engineering/EasyModbusTCP.NET
  
Copyright (c) 2024 LV Permission is hereby granted, 
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
using Grumpy.HWControl.Configuration;
using Grumpy.ModeBusHandler.Exceptions;

using System;
using System.Net.Sockets;
using System.Net;
using System.IO.Ports;



namespace Grumpy.ModeBusHandler
{
    /// <summary>
    /// Implements a ModbusClient.
    /// </summary>
    public partial class ModbusClient
    {
        public enum RegisterOrder
        {
            LowHigh = 0,
            HighLow = 1
        };

        //private int actualPositionToRead = 0;
       // private DateTime? dateTimeLastRead;

        private bool dataReceived = false;
        private bool receiveActive = false;
        private byte[]? readBuffer = new byte[256];
        private int bytesToRead = 0;

        //private bool debug=false;
        private TcpClient? _tcpClient;
        private string? _ipAddress = "127.0.0.1";
        private int _port = 502;

        private uint _transactionIdentifierInternal = 0;

        private byte []? _transactionIdentifier = new byte[2];
        private byte []? _protocolIdentifier = new byte[2];
        private byte[]? _crc = new byte[2];
        private byte []? _length = new byte[2];
        private byte _unitIdentifier = 0x01;
        private byte _functionCode;
        private byte []? _startingAddress = new byte[2];
        private byte []? _quantity = new byte[2];

        private bool _udpFlag = false;
        private int _portOut;
        private int _baudRate = 9600;
        private int _connectTimeout = 1000;
        public byte[]? _receiveData;
        public byte[]? _sendData;
        private SerialPort? _serialPort;
        private Parity _parity = Parity.Even;
        private StopBits _stopBits = StopBits.One;
        private bool _connected = false;
        private int _countRetries = 0;
        public int MaxNumberOfRetries { get; set; } = 3;

        private object _lastErrorLock = new object();   
        private string? _lastError = null;

        public string LastError {
            get {
                lock (_lastErrorLock) {
                    return (string)(_lastError?.Clone() ?? string.Empty);
                }
            }
            set {
                lock (_lastErrorLock) {
                    _lastError = (string)(value?.Clone() ?? null!);
                }
            }
        }

        public bool IsConnected => _connected;

        public delegate void ReceiveDataChangedHandler(object sender);
        public event ReceiveDataChangedHandler? ReceiveDataChanged;

        public delegate void SendDataChangedHandler(object sender);
        public event SendDataChangedHandler? SendDataChanged;

        public delegate void ConnectedChangedHandler(object sender);
        public event ConnectedChangedHandler? ConnectedChanged;

        NetworkStream? stream;

        /// <summary>
        /// Constructor which determines the Master ip-address and the Master Port.
        /// </summary>
        /// <param name="ipAddress">IP-Address of the Master device</param>
        /// <param name="port">Listening port of the Master device (should be 502)</param>
        public ModbusClient(string? ipAddress, int port)
        {
            this._ipAddress = (string)(ipAddress?.Clone() ?? string.Empty);
            this._port = port;
        }

        /// <summary>
        /// Constructor which determines the Serial-Port
        /// </summary>
        /// <param name="serialPort">Serial-Port Name e.G. "COM1"</param>
        public ModbusClient(string serialPort)
        {
            this._serialPort = new SerialPort();
            _serialPort.PortName = (string)(serialPort?.Clone() ?? string.Empty); ;
            _serialPort.BaudRate = _baudRate;
            _serialPort.Parity = _parity;
            _serialPort.StopBits = _stopBits;
            _serialPort.WriteTimeout = 10000;
            _serialPort.ReadTimeout = _connectTimeout;

            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        }

        public ModbusClient( SerialPortConfiguration sp)
        {
            _serialPort = new SerialPort();

            _baudRate = sp.BaudRate;
            _parity = sp.Parity;
            _stopBits = sp.StopBits;
            _connectTimeout = sp.WriteTimeoutMs;
            _serialPort.PortName = sp.Name;
            _serialPort.BaudRate = _baudRate;
            _serialPort.Parity = _parity;
            _serialPort.StopBits = _stopBits;
            _serialPort.WriteTimeout = sp.WriteTimeoutMs;
            _serialPort.ReadTimeout = sp.ReadTimeoutMs;
        }

        /// <summary>
        /// Parameterless constructor
        /// </summary>
        public ModbusClient()
        { }

        /// <summary>
        /// Establish connection to Master device in case of Modbus TCP. Opens COM-Port in case of Modbus RTU
        /// </summary>
        public bool Connect()
        {
            if (_serialPort != null) {
                
                if (!_serialPort.IsOpen) {
                    try {
                        _serialPort.BaudRate = _baudRate;
                        _serialPort.Parity = _parity;
                        _serialPort.StopBits = _stopBits;
                        _serialPort.WriteTimeout = 10000;
                        _serialPort.ReadTimeout = _connectTimeout;
                        _serialPort.Open();
                        _connected = true;
                    }
                    catch (Exception ex ) {
                        LastError = $"ModbusClient.{nameof(Connect)}(). Failed to open serial port {_serialPort.PortName}. Exception: {ex.Message}";
                        _connected = false; 
                    }
                }

                if (_connected && ConnectedChanged != null)
                    try {
                        ConnectedChanged(this);
                    }
                    catch {

                    }
                return _connected;
            }

            if (!_udpFlag) {
                // if (debug) StoreLogData.Instance.Store("Open TCP-Socket, IP-Address: " + ipAddress + ", Port: " + port, System.DateTime.Now);
                _tcpClient = new TcpClient();
                
                var result = _tcpClient.BeginConnect(_ipAddress!, _port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(_connectTimeout);
                if (!success) {
                    LastError = $"ModbusClient.{nameof(Connect)}(). Failed to connect to a server via {_ipAddress}:{_port} within {_connectTimeout}ms.";
                    return false;
                }
                _tcpClient.EndConnect(result);

                //tcpClient = new TcpClient(ipAddress, port);
                stream = _tcpClient.GetStream();
                stream.ReadTimeout = _connectTimeout;
                _connected = true;
            }
            else {
                _tcpClient = new TcpClient();
                _connected = true;
            }

            if (ConnectedChanged != null)
                try {
                    ConnectedChanged(this);
                }
                catch {

                }
            return _connected;
        }

        private bool _ConnectViaSerialInterface()
        {
            if (_serialPort != null) {

                if (!_serialPort.IsOpen) {
                    try {
                        _serialPort.BaudRate = _baudRate;
                        _serialPort.Parity = _parity;
                        _serialPort.StopBits = _stopBits;
                        _serialPort.WriteTimeout = 10000;
                        _serialPort.ReadTimeout = _connectTimeout;
                        _serialPort.Open();
                        _connected = true;

                        if (ConnectedChanged != null) {

                            try {
                                ConnectedChanged(this);
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex) {
                        LastError = $"ModbusClient.{nameof(_ConnectViaSerialInterface)}(). Failed to open port {_serialPort.PortName}. Exception: {ex.Message}";
                        _connected = false;
                        goto exit;
                    }
                }
            }
            else {
                _connected = false;
            }
            exit:
            return _connected;
        }

        private bool _ConnectUsingTcp(string ipAddress, int port)
        {
            _tcpClient = new TcpClient();
            IAsyncResult result = _tcpClient.BeginConnect(ipAddress, port, null, null);
            
            bool success = result.AsyncWaitHandle.WaitOne(_connectTimeout);
            
            if (!success) {

                LastError = $"ModbusClient.{nameof(_ConnectUsingTcp)}(). " +
                    $"Failed to connect to a server via " +
                    $"{ipAddress}:{port} within {_connectTimeout}ms.";

                _connected = false;
                goto exit;
            }

            _tcpClient.EndConnect(result);

            //tcpClient = new TcpClient(ipAddress, port);
            stream = _tcpClient.GetStream();
            stream.ReadTimeout = _connectTimeout;
            _connected = true;

            exit:
            return _connected;
        }

        private bool _ConnectUsingUDP(string ipAddress, int port)
        {
            _tcpClient = new TcpClient();
            _connected = true;
            return true;
        }

        /// <summary>
        /// Establish connection to Master device in case of Modbus TCP.
        /// </summary>
        public bool Connect(string ipAddress, int port)
        {
            bool res = false;
            if(_serialPort != null) {
                res =  _ConnectViaSerialInterface();
            }
            else if (!_udpFlag) {
                res = _ConnectUsingTcp(ipAddress, port);
            }
            else {
                res = _ConnectUsingUDP(ipAddress, port);
            }

            if (res && ConnectedChanged != null) {
                ConnectedChanged(this);
            }

            return res;
        }

        /// <summary>
        /// Converts two ModbusRegisters to Float - Example: EasyModbus.ModbusClient.ConvertRegistersToFloat(modbusClient.ReadHoldingRegisters(19,2))
        /// </summary>
        /// <param name="registers">Two Register values received from Modbus</param>
        /// <returns>Connected float value</returns>
        public static bool ConvertRegistersToFloat(int[]? registers, out float result, out string? error, RegisterOrder registerOrder = RegisterOrder.LowHigh)
        {
            if ((registers?.Length ?? 0) != 2) {

                error = $"ModbusClient.{nameof(ConvertRegistersToFloat)}(). Input Array length invalid - Array length must be '2'";
                result = float.NaN;
                return false;
            }

            int[] orderedRegisters = registerOrder == RegisterOrder.LowHigh ?[ registers[0], registers[1] ] : [registers[1], registers[0]];
  
            error = string.Empty;

            int highRegister = orderedRegisters[1];
            int lowRegister = orderedRegisters[0];
            byte[] highRegisterBytes = BitConverter.GetBytes(highRegister);
            byte[] lowRegisterBytes = BitConverter.GetBytes(lowRegister);
            byte[] floatBytes = {
                lowRegisterBytes[0],
                lowRegisterBytes[1],
                highRegisterBytes[0],
                highRegisterBytes[1]
            };

            result =  BitConverter.ToSingle(floatBytes, 0);
            return true;    
        }

        /// <summary>
        /// Converts two ModbusRegisters to Float, Registers can by swapped
        /// </summary>
        /// <param name="registers">Two Register values received from Modbus</param>
        /// <param name="registerOrder">Desired Word Order (Low Register first or High Register first</param>
        /// <returns>Connected float value</returns>
        public bool ConvertRegistersToFloat(int[] registers, 
            RegisterOrder registerOrder, out float result)
        {

            int [] swappedRegisters = {registers[0],registers[1]};
            if (registerOrder == RegisterOrder.HighLow) {
                swappedRegisters = new int[] { registers[1], registers[0] };
            }
            return ModbusClient.ConvertRegistersToFloat(swappedRegisters, out result, out string? error);
        }

        /// <summary>
        /// Converts two ModbusRegisters to 32 Bit Integer value
        /// </summary>
        /// <param name="registers">Two Register values received from Modbus</param>
        /// <returns>Connected 32 Bit Integer value</returns>
        public bool ConvertRegistersToInt(int[] registers, out Int32 result)
        {

            if (registers.Length != 2) {
                result = Int32.MinValue;
                LastError = $"ModbusClient.{nameof(ConvertRegistersToInt)}(). Input Array length invalid - Array length must be '2'";
                return false;
            }

            int highRegister = registers[1];
            int lowRegister = registers[0];
            byte[] highRegisterBytes = BitConverter.GetBytes(highRegister);
            byte[] lowRegisterBytes = BitConverter.GetBytes(lowRegister);
            byte[] doubleBytes = {
                                    lowRegisterBytes[0],
                                    lowRegisterBytes[1],
                                    highRegisterBytes[0],
                                    highRegisterBytes[1]
                                };
            result =  BitConverter.ToInt32(doubleBytes, 0);
            return true;
        }

        /// <summary>
        /// Converts two ModbusRegisters to 32 Bit Integer Value - Registers can be swapped
        /// </summary>
        /// <param name="registers">Two Register values received from Modbus</param>
        /// <param name="registerOrder">Desired Word Order (Low Register first or High Register first</param>
        /// <returns>Connected 32 Bit Integer value</returns>
        public bool ConvertRegistersToInt(int[] registers, RegisterOrder registerOrder, out Int32 result)
        {

            int[] swappedRegisters = { registers[0], registers[1] };
            
            if (registerOrder == RegisterOrder.HighLow) {
             
                swappedRegisters = new int[] { registers[1], registers[0] };
            }
            
            return ConvertRegistersToInt(swappedRegisters, out result);
        }

        /// <summary>
        /// Convert four 16 Bit Registers to 64 Bit Integer value Register Order "LowHigh": Reg0: Low Word.....Reg3: High Word, "HighLow": Reg0: High Word.....Reg3: Low Word
        /// </summary>
        /// <param name="registers">four Register values received from Modbus</param>
        /// <returns>64 bit value</returns>
        public bool ConvertRegistersToLong(int[] registers, out Int64 result)
        {

            if (registers.Length != 4) {
                result = Int64.MinValue;
                LastError = $"ModbusClient.{nameof(ConvertRegistersToInt)}(). Input Array length invalid - Array length must be '4'";
                return false;
            }

            int highRegister = registers[3];
            int highLowRegister = registers[2];
            int lowHighRegister = registers[1];
            int lowRegister = registers[0];

            byte[] highRegisterBytes = BitConverter.GetBytes(highRegister);
            byte[] highLowRegisterBytes = BitConverter.GetBytes(highLowRegister);
            byte[] lowHighRegisterBytes = BitConverter.GetBytes(lowHighRegister);
            byte[] lowRegisterBytes = BitConverter.GetBytes(lowRegister);
            byte[] longBytes = { lowRegisterBytes[0],
                                 lowRegisterBytes[1],
                                 lowHighRegisterBytes[0],
                                 lowHighRegisterBytes[1],
                                 highLowRegisterBytes[0],
                                 highLowRegisterBytes[1],
                                 highRegisterBytes[0],
                                 highRegisterBytes[1] };

            result =  BitConverter.ToInt64(longBytes, 0);
            return true;
        }

        /// <summary>
        /// Convert four 16 Bit Registers to 64 Bit Integer value - Registers can be swapped
        /// </summary>
        /// <param name="registers">four Register values received from Modbus</param>
        /// <param name="registerOrder">Desired Word Order (Low Register first or High Register first</param>
        /// <returns>Connected 64 Bit Integer value</returns>
        public bool ConvertRegistersToLong(int[] registers, 
            RegisterOrder registerOrder, out Int64 result)
        {

            if (registers.Length != 4) {
                result= Int64.MinValue;
                LastError = $"ModbusClient.{nameof(ConvertRegistersToLong)}(). Input Array length invalid - Array length must be '4'";
                return false;
            }
            int[] swappedRegisters = { registers[0], registers[1], registers[2], registers[3] };
            
            if (registerOrder == RegisterOrder.HighLow) {
                swappedRegisters = new int[] { registers[3], registers[2], registers[1], registers[0] };
            }

            return  ConvertRegistersToLong(swappedRegisters, out result);
        }

        /// <summary>
        /// Convert four 16 Bit Registers to 64 Bit double prec. value Register Order "LowHigh": Reg0: Low Word.....Reg3: High Word, "HighLow": Reg0: High Word.....Reg3: Low Word
        /// </summary>
        /// <param name="registers">four Register values received from Modbus</param>
        /// <returns>64 bit value</returns>
        public bool ConvertRegistersToDouble(int[] registers, out double result)
        {

            if (registers.Length != 4) {
                result = Double.NaN;
                LastError = $"ModbusClient.{nameof(ConvertRegistersToDouble)}(). Input Array length invalid - Array length must be '4'";
                return false;
            }

            int highRegister = registers[3];
            int highLowRegister = registers[2];
            int lowHighRegister = registers[1];
            int lowRegister = registers[0];
            byte[] highRegisterBytes = BitConverter.GetBytes(highRegister);
            byte[] highLowRegisterBytes = BitConverter.GetBytes(highLowRegister);
            byte[] lowHighRegisterBytes = BitConverter.GetBytes(lowHighRegister);
            byte[] lowRegisterBytes = BitConverter.GetBytes(lowRegister);
            byte[] longBytes = {
                                    lowRegisterBytes[0],
                                    lowRegisterBytes[1],
                                    lowHighRegisterBytes[0],
                                    lowHighRegisterBytes[1],
                                    highLowRegisterBytes[0],
                                    highLowRegisterBytes[1],
                                    highRegisterBytes[0],
                                    highRegisterBytes[1]
                                };
            result =  BitConverter.ToDouble(longBytes, 0);
            return true;
        }

        /// <summary>
        /// Convert four 16 Bit Registers to 64 Bit double prec. value - Registers can be swapped
        /// </summary>
        /// <param name="registers">four Register values received from Modbus</param>
        /// <param name="registerOrder">Desired Word Order (Low Register first or High Register first</param>
        /// <returns>Connected double prec. float value</returns>
        public bool ConvertRegistersToDouble(int[] registers, 
            RegisterOrder registerOrder, out double result)
        {

            if (registers.Length != 4) {
                result = Double.NaN;
                LastError = $"ModbusClient.{nameof(ConvertRegistersToDouble)}(). Input Array length invalid - Array length must be '4'";
                return false;
            }
            
            int[] swappedRegisters = { registers[0], registers[1], registers[2], registers[3] };
            
            if (registerOrder == RegisterOrder.HighLow) {
                swappedRegisters = new int[] { registers[3], registers[2], registers[1], registers[0] };
            }
            
            return ConvertRegistersToDouble(swappedRegisters, out result);
        }

        /// <summary>
        /// Converts float to two ModbusRegisters - Example:  modbusClient.WriteMultipleRegisters(24, EasyModbus.ModbusClient.ConvertFloatToTwoRegisters((float)1.22));
        /// </summary>
        /// <param name="floatValue">Float value which has to be converted into two registers</param>
        /// <returns>Register values</returns>
        public static int[] ConvertFloatToRegisters(float floatValue)
        {

            byte[] floatBytes = BitConverter.GetBytes(floatValue);
            byte[] highRegisterBytes =
            {
                floatBytes[2],
                floatBytes[3],
                0,
                0
            };
            byte[] lowRegisterBytes =
            {

                floatBytes[0],
                floatBytes[1],
                0,
                0
            };
            int[] returnValue =
            {
                BitConverter.ToInt32(lowRegisterBytes,0),
                BitConverter.ToInt32(highRegisterBytes,0)
            };
            return returnValue;
        }

        /// <summary>
        /// Converts float to two ModbusRegisters Registers - Registers can be swapped
        /// </summary>
        /// <param name="floatValue">Float value which has to be converted into two registers</param>
        /// <param name="registerOrder">Desired Word Order (Low Register first or High Register first</param>
        /// <returns>Register values</returns>
        public static int[] ConvertFloatToRegisters(float floatValue, RegisterOrder registerOrder)
        {

            int[] registerValues = ConvertFloatToRegisters(floatValue);
            int[] returnValue = registerValues;
            if (registerOrder == RegisterOrder.HighLow)
                returnValue = new Int32[] { registerValues[1], registerValues[0] };
            return returnValue;
        }

        /// <summary>
        /// Converts 32 Bit Value to two ModbusRegisters
        /// </summary>
        /// <param name="intValue">Int value which has to be converted into two registers</param>
        /// <returns>Register values</returns>
        public static int[] ConvertIntToRegisters(Int32 intValue)
        {

            byte[] doubleBytes = BitConverter.GetBytes(intValue);
            byte[] highRegisterBytes =
            {
                doubleBytes[2],
                doubleBytes[3],
                0,
                0
            };
            byte[] lowRegisterBytes =
            {

                doubleBytes[0],
                doubleBytes[1],
                0,
                0
            };
            int[] returnValue =
            {
                BitConverter.ToInt32(lowRegisterBytes,0),
                BitConverter.ToInt32(highRegisterBytes,0)
            };
            return returnValue;
        }

        /// <summary>
        /// Converts 32 Bit Value to two ModbusRegisters Registers - Registers can be swapped
        /// </summary>
        /// <param name="intValue">Double value which has to be converted into two registers</param>
        /// <param name="registerOrder">Desired Word Order (Low Register first or High Register first</param>
        /// <returns>Register values</returns>
        public static int[] ConvertIntToRegisters(Int32 intValue, RegisterOrder registerOrder)
        {

            int[] registerValues = ConvertIntToRegisters(intValue);
            int[] returnValue = registerValues;
            if (registerOrder == RegisterOrder.HighLow)
                returnValue = new Int32[] { registerValues[1], registerValues[0] };
            return returnValue;
        }

        /// <summary>
        /// Converts 64 Bit Value to four ModbusRegisters
        /// </summary>
        /// <param name="longValue">long value which has to be converted into four registers</param>
        /// <returns>Register values</returns>
        public static int[] ConvertLongToRegisters(Int64 longValue)
        {

            byte[] longBytes = BitConverter.GetBytes(longValue);
            byte[] highRegisterBytes =
            {
                longBytes[6],
                longBytes[7],
                0,
                0
            };
            byte[] highLowRegisterBytes =
            {
                longBytes[4],
                longBytes[5],
                0,
                0
            };
            byte[] lowHighRegisterBytes =
            {
                longBytes[2],
                longBytes[3],
                0,
                0
            };
            byte[] lowRegisterBytes =
            {

                longBytes[0],
                longBytes[1],
                0,
                0
            };
            int[] returnValue =
            {
                BitConverter.ToInt32(lowRegisterBytes,0),
                BitConverter.ToInt32(lowHighRegisterBytes,0),
                BitConverter.ToInt32(highLowRegisterBytes,0),
                BitConverter.ToInt32(highRegisterBytes,0)
            };
            return returnValue;
        }

        /// <summary>
        /// Converts 64 Bit Value to four ModbusRegisters - Registers can be swapped
        /// </summary>
        /// <param name="longValue">long value which has to be converted into four registers</param>
        /// <param name="registerOrder">Desired Word Order (Low Register first or High Register first</param>
        /// <returns>Register values</returns>
        public static int[] ConvertLongToRegisters(Int64 longValue, RegisterOrder registerOrder)
        {

            int[] registerValues = ConvertLongToRegisters(longValue);
            int[] returnValue = registerValues;
            if (registerOrder == RegisterOrder.HighLow)
                returnValue = new int[] { registerValues[3], registerValues[2], registerValues[1], registerValues[0] };
            return returnValue;
        }

        /// <summary>
        /// Converts 64 Bit double prec Value to four ModbusRegisters
        /// </summary>
        /// <param name="doubleValue">double value which has to be converted into four registers</param>
        /// <returns>Register values</returns>
        public static int[] ConvertDoubleToRegisters(double doubleValue)
        {

            byte[] doubleBytes = BitConverter.GetBytes(doubleValue);
            byte[] highRegisterBytes =
            {
                doubleBytes[6],
                doubleBytes[7],
                0,
                0
            };
            byte[] highLowRegisterBytes =
            {
                doubleBytes[4],
                doubleBytes[5],
                0,
                0
            };
            byte[] lowHighRegisterBytes =
            {
                doubleBytes[2],
                doubleBytes[3],
                0,
                0
            };
            byte[] lowRegisterBytes =
            {

                doubleBytes[0],
                doubleBytes[1],
                0,
                0
            };
            int[] returnValue =
            {
                BitConverter.ToInt32(lowRegisterBytes,0),
                BitConverter.ToInt32(lowHighRegisterBytes,0),
                BitConverter.ToInt32(highLowRegisterBytes,0),
                BitConverter.ToInt32(highRegisterBytes,0)
            };
            return returnValue;
        }

        /// <summary>
        /// Converts 64 Bit double prec. Value to four ModbusRegisters - Registers can be swapped
        /// </summary>
        /// <param name="doubleValue">double value which has to be converted into four registers</param>
        /// <param name="registerOrder">Desired Word Order (Low Register first or High Register first</param>
        /// <returns>Register values</returns>
        public static int[] ConvertDoubleToRegisters(double doubleValue, RegisterOrder registerOrder)
        {

            int[] registerValues = ConvertDoubleToRegisters(doubleValue);
            int[] returnValue = registerValues;
            if (registerOrder == RegisterOrder.HighLow)
                returnValue = new int[] { registerValues[3], registerValues[2], registerValues[1], registerValues[0] };
            return returnValue;
        }

        /// <summary>
        /// Converts 16 - Bit Register values to String
        /// </summary>
        /// <param name="registers">Register array received via Modbus</param>
        /// <param name="offset">First Register containing the String to convert</param>
        /// <param name="stringLength">number of characters in String (must be even)</param>
        /// <returns>Converted String</returns>
        public static string ConvertRegistersToString(int[] registers, int offset, int stringLength)
        {

            byte[] result = new byte[stringLength];
            byte[] registerResult = new byte[2];

            for (int i = 0; i < stringLength / 2; i++) {
                registerResult = BitConverter.GetBytes(registers[offset + i]);
                result[i * 2] = registerResult[0];
                result[i * 2 + 1] = registerResult[1];
            }
            return System.Text.Encoding.Default.GetString(result);
        }

        /// <summary>
        /// Converts a String to 16 - Bit Registers
        /// </summary>
        /// <param name="registers">Register array received via Modbus</param>
        /// <returns>Converted String</returns>
        public static int[] ConvertStringToRegisters(string stringToConvert)
        {

            byte[] array = System.Text.Encoding.ASCII.GetBytes(stringToConvert);
            int[] returnArray = new int[stringToConvert.Length / 2 + stringToConvert.Length % 2];
            for (int i = 0; i < returnArray.Length; i++) {
                returnArray[i] = array[i * 2];
                if (i * 2 + 1 < array.Length) {
                    returnArray[i] = returnArray[i] | ((int)array[i * 2 + 1] << 8);
                }
            }
            return returnArray;
        }


        /// <summary>
        /// Calculates the CRC16 for Modbus-RTU
        /// </summary>
        /// <param name="data">Byte buffer to send</param>
        /// <param name="numberOfBytes">Number of bytes to calculate CRC</param>
        /// <param name="startByte">First byte in buffer to start calculating CRC</param>
        public static UInt16 calculateCRC(byte[] data, UInt16 numberOfBytes, int startByte)
        {
            byte[] auchCRCHi = {
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
            0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81,
            0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
            0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
            0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
            0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40
            };

            byte[] auchCRCLo = {
            0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 0x05, 0xC5, 0xC4,
            0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09,
            0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD,
            0x1D, 0x1C, 0xDC, 0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
            0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32, 0x36, 0xF6, 0xF7,
            0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A,
            0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE,
            0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
            0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1, 0x63, 0xA3, 0xA2,
            0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F,
            0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8, 0xB9, 0x79, 0xBB,
            0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
            0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0, 0x50, 0x90, 0x91,
            0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C,
            0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98, 0x88,
            0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
            0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80,
            0x40
            };
            UInt16 usDataLen = numberOfBytes;
            byte  uchCRCHi = 0xFF ;
            byte uchCRCLo = 0xFF ;
            int i = 0;
            int uIndex ;
            while (usDataLen > 0) {
                usDataLen--;
                if ((i + startByte) < data.Length) {
                    uIndex = uchCRCLo ^ data[i + startByte];
                    uchCRCLo = (byte)(uchCRCHi ^ auchCRCHi[uIndex]);
                    uchCRCHi = auchCRCLo[uIndex];
                }
                i++;
            }
            return (UInt16)((UInt16)uchCRCHi << 8 | uchCRCLo);
        }

        private void DataReceivedHandler(object sender,
                        SerialDataReceivedEventArgs e)
        {
            _serialPort!.DataReceived -= DataReceivedHandler;

            //while (receiveActive | dataReceived)
            //	System.Threading.Thread.Sleep(10);
            receiveActive = true;

            const long ticksWait = TimeSpan.TicksPerMillisecond * 2000;//((40*10000000) / this.baudRate);


            SerialPort sp = (SerialPort)sender;
            if (bytesToRead == 0) {
                sp.DiscardInBuffer();
                receiveActive = false;
                _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                return;
            }
            readBuffer = new byte[256];
            int numBytes=0;
            int actualPositionToRead = 0;
            DateTime dateTimeLastRead = DateTime.Now;
            do {
                try {
                    dateTimeLastRead = DateTime.Now;
                    while ((sp.BytesToRead) == 0) {
                        System.Threading.Thread.Sleep(10);
                        if ((DateTime.Now.Ticks - dateTimeLastRead.Ticks) > ticksWait)
                            break;
                    }
                    numBytes = sp.BytesToRead;


                    byte[] rxByteArray = new byte[numBytes];
                    sp.Read(rxByteArray, 0, numBytes);
                    Array.Copy(rxByteArray, 0, readBuffer, actualPositionToRead, 
                        (actualPositionToRead + rxByteArray.Length) <= bytesToRead 
                        ? rxByteArray.Length 
                        : bytesToRead - actualPositionToRead);

                    actualPositionToRead = actualPositionToRead + rxByteArray.Length;

                }
                catch (Exception) {

                }

                if (bytesToRead <= actualPositionToRead)
                    break;

                if (DetectValidModbusFrame(readBuffer, 
                    (actualPositionToRead < readBuffer.Length) 
                    ? actualPositionToRead 
                    : readBuffer.Length) | bytesToRead <= actualPositionToRead)
                    break;
            }

            while ((DateTime.Now.Ticks - dateTimeLastRead.Ticks) < ticksWait);

            //10.000 Ticks in 1 ms

            _receiveData = new byte[actualPositionToRead];
            Array.Copy(readBuffer, 0, _receiveData, 0, 
                (actualPositionToRead < readBuffer.Length) 
                ? actualPositionToRead 
                : readBuffer.Length);
            // if (debug) StoreLogData.Instance.Store("Received Serial-Data: "+BitConverter.ToString(readBuffer) ,System.DateTime.Now);
            bytesToRead = 0;


            dataReceived = true;
            receiveActive = false;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            if (ReceiveDataChanged != null) {

                ReceiveDataChanged(this);

            }

            //sp.DiscardInBuffer();
        }

        public static bool DetectValidModbusFrame(byte[] readBuffer, int length)
        {
            // minimum length 6 bytes
            if (length < 6) {
                return false;
            }
            //SlaveID correct
            if ((readBuffer[0] < 1) | (readBuffer[0] > 247)) { 
                return false; 
            }
            //CRC correct?
            byte[] crc = new byte[2];
            crc = BitConverter.GetBytes(calculateCRC(readBuffer, (ushort)(length - 2), 0));
            if (crc[0] != readBuffer[length - 2] | crc[1] != readBuffer[length - 1]) { 
                return false; 
            }

            return true;
        }

        protected bool Success(byte[] data, byte mask) {

            if (data[7] == mask) {

                switch (data[8]) {
                    case (0x01):
                        LastError = "Transaction error check. Function code not supported by the master";
                        return false;
                    case (0x02):
                        LastError = "Transaction error check. Starting address invalid or starting address + quantity invalid";
                        return false;
                    case (0x03):
                        LastError = "Transaction error check. Quantity invalid";
                        return false;
                    case (0x04):
                        LastError = "Transaction error check. Error reading";
                        return false;
                    default:
                        return true;
                }
            }

            return true;
        }

        /// <summary>
        /// Read Discrete Inputs from Server device (FC2).
        /// </summary>
        /// <param name="startingAddress">First discrete input to read</param>
        /// <param name="quantity">Number of discrete Inputs to read</param>
        ///  <param name="functionCode">Function code to be used in the ModBusTransaction. Optional. Set to 0x02 by deafult.</param>
        /// <returns>Boolean Array which contains the discrete Inputs</returns>
        public bool ReadDiscreteInputs(int startingAddress, int quantity, out bool[]? response, int functionCode = 0x02)
        {
            response = null;
            // if (debug) StoreLogData.Instance.Store("FC2 (Read Discrete Inputs from Master device), StartingAddress: "+ startingAddress+", Quantity: " +quantity, System.DateTime.Now);
            _transactionIdentifierInternal++;
            if (_serialPort != null) {
                if (!_serialPort.IsOpen) {

                    LastError = $"ModbusClient.{nameof(ReadDiscreteInputs)}(). Serial port is not opened";
                    return false;
                }
            }

            if (_tcpClient == null & !_udpFlag & _serialPort == null) {
                
                LastError = $"ModbusClient.{nameof(ReadDiscreteInputs)}(). TCP connection error";
                return false;
            }
            if (startingAddress > 65535 | quantity > 2000) {

                LastError = $"ModbusClient.{nameof(ReadDiscreteInputs)}(). Starting address must be 0 - 65535 " +
                    $"({startingAddress}); quantity must be 0 - 2000 ({quantity})";
                return false;
            }

            this._transactionIdentifier = BitConverter.GetBytes((uint)_transactionIdentifierInternal);
            this._protocolIdentifier = BitConverter.GetBytes((int)0x0000);
            this._length = BitConverter.GetBytes((int)0x0006);
            this._functionCode = (byte) functionCode;
            this._startingAddress = BitConverter.GetBytes(startingAddress);
            this._quantity = BitConverter.GetBytes(quantity);

            Byte[] data = new byte[]
                            {
                            this._transactionIdentifier[1],
                            this._transactionIdentifier[0],
                            this._protocolIdentifier[1],
                            this._protocolIdentifier[0],
                            this._length[1],
                            this._length[0],
                            this._unitIdentifier,
                            this._functionCode,
                            this._startingAddress[1],
                            this._startingAddress[0],
                            this._quantity[1],
                            this._quantity[0],
                            this._crc![0],
                            this._crc[1]
                            };
            _crc = BitConverter.GetBytes(calculateCRC(data, 6, 6));
            data[12] = _crc[0];
            data[13] = _crc[1];

            if (_serialPort != null) {

                dataReceived = false;

                if (quantity % 8 == 0) {
                    bytesToRead = 5 + quantity / 8;
                }
                else {
                    bytesToRead = 6 + quantity / 8;
                    //               serialport.ReceivedBytesThreshold = bytesToRead;
                }
                if (SendDataChanged != null) {
                 
                    _sendData = new byte[8];
                    Array.Copy(data, 6, _sendData, 0, 8);
                    SendDataChanged(this);
                }

                data = new byte[2100];
                readBuffer = new byte[256];
                DateTime dateTimeSend = DateTime.Now;
                byte receivedUnitIdentifier = 0xFF;

                while (receivedUnitIdentifier != this._unitIdentifier 
                      & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout)) {

                    while (dataReceived == false & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout)) {
                     
                        System.Threading.Thread.Sleep(1);
                    }

                    data = new byte[2100];
                    Array.Copy(readBuffer, 0, data, 6, readBuffer.Length);
                    receivedUnitIdentifier = data[6];
                }
                if (receivedUnitIdentifier != this._unitIdentifier)
                    data = new byte[2100];
                else
                    _countRetries = 0;
            }
            else if (_tcpClient!.Client.Connected | _udpFlag) {
 
                if (_udpFlag) {
                
                    UdpClient udpClient = new UdpClient();
                    IPEndPoint endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress == null ? string.Empty : _ipAddress), _port);
                    udpClient.Send(data, data.Length - 2, endPoint);
                    _portOut = ((IPEndPoint)(udpClient?.Client?.LocalEndPoint!)).Port;
                    udpClient.Client.ReceiveTimeout = 5000;
                    endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress == null ? string.Empty : _ipAddress), _portOut);
                    data = udpClient.Receive(ref endPoint);
                }
                else {
                    if( stream == null) {
                        LastError = $"ModbusClient.{nameof(ReadDiscreteInputs)}(). Tcp stream is not defined.";
                        return false;
                    }
                    stream!.Write(data, 0, data.Length - 2);

                    if (SendDataChanged != null) {
                    
                        _sendData = new byte[data.Length - 2];
                        Array.Copy(data, 0, _sendData, 0, data.Length - 2);
                        SendDataChanged(this);
                    }
                    data = new Byte[2100];
                    int NumberOfBytes = stream.Read(data, 0, data.Length);
                    
                    if (ReceiveDataChanged != null) {
                    
                        _receiveData = new byte[NumberOfBytes];
                        Array.Copy(data, 0, _receiveData, 0, NumberOfBytes);
                        // if (debug) StoreLogData.Instance.Store("Receive ModbusTCP-Data: " + BitConverter.ToString(receiveData), System.DateTime.Now);
                        ReceiveDataChanged(this);
                    }
                }
            }

            if(!Success(data, 0x82)) { return false; }


            if (_serialPort != null) {

                _crc = BitConverter.GetBytes(calculateCRC(data, (ushort)(data[8] + 3), 6));
                
                if ((_crc[0] != data[data[8] + 9] | _crc[1] != data[data[8] + 10]) & dataReceived) {
                         
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        LastError = $"ModbusClient.{nameof(ReadDiscreteInputs)}().Response CRC check failed";
                        return false;
                    }
                    else {
                        _countRetries++;
                        return ReadDiscreteInputs(startingAddress, quantity, out response, functionCode);
                    }
                }
                else if (!dataReceived) {
                   
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        LastError = $"ModbusClient.{nameof(ReadDiscreteInputs)}(). No Response from Modbus Slave";
                        return false;
                    }
                    else {
                        _countRetries++;
                        return ReadDiscreteInputs(startingAddress, quantity, out response, functionCode);
                    }
                }
            }
            response = new bool[quantity];
            
            for (int i = 0; i < quantity; i++) {
            
                int intData = data[9+i/8];
                int mask = Convert.ToInt32(Math.Pow(2, (i%8)));
                response[i] = Convert.ToBoolean((intData & mask) / mask);
            }

            return true;
        }


        /// <summary>
        /// Read Coils from Server device (FC1).
        /// </summary>
        /// <param name="startingAddress">First coil address to read</param>
        /// <param name="quantity">Number of coils to read</param>
        ///  <param name="functionCode">Function code to be used in the ModBusTransaction. Optional. Set to 0x01 by deafult.</param>
        /// <returns>Boolean Array which contains the coils</returns>
        public bool ReadCoils(int startingAddress, int quantity, out bool[]? coils, int functionCode = 0x01)
        {
            coils = null;
            _transactionIdentifierInternal++;

            if (_serialPort != null) {
                if (!_serialPort.IsOpen) {
                     LastError = "serial port not opened";
                    return false;
                }
            }

            if (_tcpClient == null & !_udpFlag & _serialPort == null) {

                LastError = "connection error";
                return false;
            }

           if (startingAddress > 65535 | quantity > 2000) {

               LastError = "Starting address must be 0 - 65535; quantity must be 0 - 2000";
               return false;
           }
  
            this._transactionIdentifier = BitConverter.GetBytes((uint)_transactionIdentifierInternal);
            this._protocolIdentifier = BitConverter.GetBytes((int)0x0000);
            this._length = BitConverter.GetBytes((int)0x0006);
            this._functionCode = (byte) functionCode;
            this._startingAddress = BitConverter.GetBytes(startingAddress);
            this._quantity = BitConverter.GetBytes(quantity);
            Byte[] data = new byte[]{
                            this._transactionIdentifier[1],
                            this._transactionIdentifier[0],
                            this._protocolIdentifier[1],
                            this._protocolIdentifier[0],
                            this._length[1],
                            this._length[0],
                            this._unitIdentifier,
                            this._functionCode,
                            this._startingAddress[1],
                            this._startingAddress[0],
                            this._quantity[1],
                            this._quantity[0],
                            this._crc![0],
                            this._crc[1]
            };

            _crc = BitConverter.GetBytes(calculateCRC(data, 6, 6));
            data[12] = _crc[0];
            data[13] = _crc[1];
            if (_serialPort != null) {
                dataReceived = false;
                if (quantity % 8 == 0)
                    bytesToRead = 5 + quantity / 8;
                else
                    bytesToRead = 6 + quantity / 8;
                //               serialport.ReceivedBytesThreshold = bytesToRead;
                _serialPort.Write(data, 6, 8);

                if (SendDataChanged != null) {
                    _sendData = new byte[8];
                    Array.Copy(data, 6, _sendData, 0, 8);
                    SendDataChanged(this);

                }
                data = new byte[2100];
                readBuffer = new byte[256];
                DateTime dateTimeSend = DateTime.Now;
                byte receivedUnitIdentifier = 0xFF;
                while (receivedUnitIdentifier != this._unitIdentifier & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout)) {
                    while (dataReceived == false & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout))
                        System.Threading.Thread.Sleep(1);
                    data = new byte[2100];

                    Array.Copy(readBuffer, 0, data, 6, readBuffer.Length);
                    receivedUnitIdentifier = data[6];
                }
                if (receivedUnitIdentifier != this._unitIdentifier)
                    data = new byte[2100];
                else
                    _countRetries = 0;
            }
            else if ((_tcpClient?.Client?.Connected ?? false)| _udpFlag) {
                if (_udpFlag) {

                    UdpClient udpClient = new UdpClient();
                    IPEndPoint endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress == null ? String.Empty: _ipAddress), _port);

                    udpClient.Send(data, data.Length - 2, endPoint);
                   
                    if (udpClient?.Client?.LocalEndPoint != null) {

                        _portOut = ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
                        udpClient.Client.ReceiveTimeout = 5000;
                        endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress == null ? String.Empty : _ipAddress), _portOut);
                        data = udpClient.Receive(ref endPoint);
                    }
                    else {
                        throw (new ConnectionException("Local endpoint not defined for UDP connection."));
                    }

                }
                else {
                    if (stream == null) {
                        throw (new ConnectionException("TCP connection stream not defined."));
                    }
                    stream.Write(data, 0, data.Length - 2);

                    if (SendDataChanged != null) {
                        _sendData = new byte[data.Length - 2];
                        Array.Copy(data, 0, _sendData, 0, data.Length - 2);
                        SendDataChanged(this);

                    }
                    data = new Byte[2100];
                    int NumberOfBytes = stream.Read(data, 0, data.Length);
                    if (ReceiveDataChanged != null) {
                        _receiveData = new byte[NumberOfBytes];
                        Array.Copy(data, 0, _receiveData, 0, NumberOfBytes);
                        // if (debug) StoreLogData.Instance.Store("Receive ModbusTCP-Data: " + BitConverter.ToString(receiveData), System.DateTime.Now);
                        ReceiveDataChanged(this);
                    }
                }
            }
            if (data[7] == 0x81 & data[8] == 0x01) {

                LastError = "Function code not supported by master";
                return false;
            }
            if (data[7] == 0x81 & data[8] == 0x02) {
                LastError = "Starting address invalid or starting address + quantity invalid";
                return false;
            }
            if (data[7] == 0x81 & data[8] == 0x03) {
                LastError = "quantity invalid";
                return false;
            }
            if (data[7] == 0x81 & data[8] == 0x04) {
                LastError = "error reading";
                return false;
            }
            if (_serialPort != null) {

                _crc = BitConverter.GetBytes(calculateCRC(data, (ushort)(data[8] + 3), 6));
                
                if ((_crc[0] != data[data[8] + 9] | _crc[1] != data[data[8] + 10]) & dataReceived) {
                    // if (debug) StoreLogData.Instance.Store("CRCCheckFailedException Throwed", System.DateTime.Now);
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        throw new ModeBusHandler.Exceptions.CRCCheckFailedException("Response CRC check failed");
                    }
                    else {
                        _countRetries++;
                        return ReadCoils(startingAddress, quantity, out coils, functionCode);
                    }
                }
                else if (!dataReceived) {
                    // if (debug) StoreLogData.Instance.Store("TimeoutException Throwed", System.DateTime.Now);
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        LastError = "No Response from Modbus Slave";
                        return false;
                    }
                    else {
                        _countRetries++;
                        return ReadCoils(startingAddress, quantity, out coils, functionCode);
                    }
                }
            }
            coils = new bool[quantity];
            for (int i = 0; i < quantity; i++) {
                int intData = data[9+i/8];
                int mask = Convert.ToInt32(Math.Pow(2, (i%8)));
                coils[i] = Convert.ToBoolean((intData & mask) / mask);
            }
            return true;
        }

        /// <summary>
        /// Read Holding Registers from Master device (FC3).
        /// </summary>
        /// <param name="startingAddress">First holding register to be read</param>
        /// <param name="quantity">Number of holding registers to be read</param>
        /// <param name="functionCode">Function code to be used in the ModBusTransaction. Optional. Set to 0x03 by deafult.</param>
        /// <returns>Int Array which contains the holding registers</returns>
        public bool ReadHoldingRegisters(int startingAddress, int quantity, out int[]? registers, int functionCode = 0x03)
        {
            registers = null;
            _transactionIdentifierInternal++;

            if (_serialPort != null) {
                if (!_serialPort.IsOpen) {

                    LastError = "serial port not opened";
                    return false;
                }
            }

            if (_tcpClient == null & !_udpFlag & _serialPort == null) {
               
                LastError = "connection error";
                return false;
            }
            if (startingAddress > 65535 | quantity > 125) {
                
                LastError = "Starting address must be 0 - 65535; quantity must be 0 - 125";
                return false;
            }
    
            this._transactionIdentifier = BitConverter.GetBytes((uint)_transactionIdentifierInternal);
            this._protocolIdentifier = BitConverter.GetBytes((int)0x0000);
            this._length = BitConverter.GetBytes((int)0x0006);
            this._functionCode = (byte) functionCode;
            this._startingAddress = BitConverter.GetBytes(startingAddress);
            this._quantity = BitConverter.GetBytes(quantity);
            Byte[] data = new byte[]{   this._transactionIdentifier[1],
                            this._transactionIdentifier[0],
                            this._protocolIdentifier[1],
                            this._protocolIdentifier[0],
                            this._length[1],
                            this._length[0],
                            this._unitIdentifier,
                            this._functionCode,
                            this._startingAddress[1],
                            this._startingAddress[0],
                            this._quantity[1],
                            this._quantity[0],
                            this._crc![0],
                            this._crc[1]
            };
            _crc = BitConverter.GetBytes(calculateCRC(data, 6, 6));
            data[12] = _crc[0];
            data[13] = _crc[1];
            if (_serialPort != null) {
                dataReceived = false;
                bytesToRead = 5 + 2 * quantity;
                //                serialport.ReceivedBytesThreshold = bytesToRead;
                _serialPort.Write(data, 6, 8);

                if (SendDataChanged != null) {
                    _sendData = new byte[8];
                    Array.Copy(data, 6, _sendData, 0, 8);
                    SendDataChanged(this);

                }
                data = new byte[2100];
                readBuffer = new byte[256];

                DateTime dateTimeSend = DateTime.Now;
                byte receivedUnitIdentifier = 0xFF;
                while (receivedUnitIdentifier != this._unitIdentifier & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout)) {
                    while (dataReceived == false & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout))
                        System.Threading.Thread.Sleep(1);
                    data = new byte[2100];
                    Array.Copy(readBuffer, 0, data, 6, readBuffer.Length);

                    receivedUnitIdentifier = data[6];
                }
                if (receivedUnitIdentifier != this._unitIdentifier)
                    data = new byte[2100];
                else
                    _countRetries = 0;
            }
            else if (( _tcpClient?.Client?.Connected ?? false) | _udpFlag) {

                if (_udpFlag) {

                    UdpClient udpClient = new UdpClient();
                    IPEndPoint endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress == null ? string.Empty : _ipAddress), _port);
                    udpClient.Send(data, data.Length - 2, endPoint);
                    _portOut = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;
                    udpClient.Client.ReceiveTimeout = 5000;
                    endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress == null ? string.Empty : _ipAddress), _portOut);
                    data = udpClient.Receive(ref endPoint);
                }
                else {
                    if ( stream == null) {
                        throw (new Exception("Tcp stream is not defined."));
                    }
                    stream.Write(data, 0, data.Length - 2);

                    if (SendDataChanged != null) {
                        _sendData = new byte[data.Length - 2];
                        Array.Copy(data, 0, _sendData, 0, data.Length - 2);
                        SendDataChanged(this);

                    }
                    data = new Byte[256];
                    int NumberOfBytes = stream.Read(data, 0, data.Length);
                    if (ReceiveDataChanged != null) {
                        _receiveData = new byte[NumberOfBytes];
                        Array.Copy(data, 0, _receiveData, 0, NumberOfBytes);
                        // if (debug) StoreLogData.Instance.Store("Receive ModbusTCP-Data: " + BitConverter.ToString(receiveData), System.DateTime.Now);
                        ReceiveDataChanged(this);
                    }
                }
            }
            if (data[7] == 0x83 & data[8] == 0x01) {
                
                LastError = "Function code not supported by master";
                return false;
            }
            if (data[7] == 0x83 & data[8] == 0x02) {

                LastError = "Starting address invalid or starting address + quantity invalid";
                return false;
            }
            if (data[7] == 0x83 & data[8] == 0x03) {

                LastError = "quantity invalid";
                return false;
            }
            if (data[7] == 0x83 & data[8] == 0x04) {

                LastError = "error reading";
                return false;
            }
            if (_serialPort != null) {
                
                _crc = BitConverter.GetBytes(calculateCRC(data, (ushort)(data[8] + 3), 6));
                
                if ((_crc[0] != data[data[8] + 9] | _crc[1] != data[data[8] + 10]) & dataReceived) {
                   
                    if (MaxNumberOfRetries <= _countRetries) {
                        
                        _countRetries = 0;
                        LastError = "Response CRC check failed";
                        return false;
                    }
                    else {
                        _countRetries++;
                        return ReadHoldingRegisters(startingAddress, quantity, out registers);
                    }
                }
                else if (!dataReceived) {
                    // if (debug) StoreLogData.Instance.Store("TimeoutException Throwed", System.DateTime.Now);
                    if (MaxNumberOfRetries <= _countRetries) {
                        
                        _countRetries = 0;
                        LastError = "No Response from Modbus Slave";
                        return false;
                    }
                    else {
                        _countRetries++;
                        return ReadHoldingRegisters(startingAddress, quantity, out registers);
                    }
                }
            }

            registers = new int[quantity];
            
            for (int i = 0; i < quantity; i++) {
                byte lowByte;
                byte highByte;
                highByte = data[9 + i * 2];
                lowByte = data[9 + i * 2 + 1];

                data[9 + i * 2] = lowByte;
                data[9 + i * 2 + 1] = highByte;

                registers[i] = BitConverter.ToInt16(data, (9 + i * 2));
            }

            return true;
        }

        public bool ReadSingle16bitRegister( int address, out ushort value, int functionCode = 0x03)
        {
            if ( ReadInputRegisters(address, 1 , out int[]? response, functionCode)) {
                value = (ushort) response![0];
                return true;
            }
            value = ushort.MaxValue;
            return false;
        }

        public bool ReadSingle32bitRegister(int address, out ushort value, int functionCode = 0x03)
        {
            if (ReadInputRegisters(address, 2, out int[]? response, functionCode)) {

               
                value = (ushort)response![0];
                return true;
            }
            value = ushort.MaxValue;
           return false;
        }

        /// <summary>
        /// Read Input Registers from Master device (FC4).
        /// </summary>
        /// <param name="startingAddress">First input register to be read</param>
        /// <param name="quantity">Number of input registers to be read</param>
        /// <param name="functionCode">Function code to be used in the ModBusTransaction. Optional. Set to 0x03 by deafult.</param>
        /// <returns>Int Array which contains the input registers</returns>
        public bool ReadInputRegisters(int startingAddress, int quantity, out int[]? response, int functionCode = 0x03)
        {
            response = null;

            _transactionIdentifierInternal++;

            if (_serialPort != null)
                if (!_serialPort.IsOpen) {
                    
                    LastError = "serial port not opened";
                    return false;
                }
            if (_tcpClient == null & !_udpFlag & _serialPort == null) {
                
                LastError = "connection error";
                return false;
            }
            if (startingAddress > 65535 | quantity > 125) {
                
                LastError = "Starting address must be 0 - 65535; quantity must be 0 - 125";
                return false;
            }

            this._transactionIdentifier = BitConverter.GetBytes((uint)_transactionIdentifierInternal);
            this._protocolIdentifier = BitConverter.GetBytes((int)0x0000);
            this._length = BitConverter.GetBytes((int)0x0006);
            this._functionCode = (byte)functionCode;
            this._startingAddress = BitConverter.GetBytes(startingAddress);
            this._quantity = BitConverter.GetBytes(quantity);
            Byte[] data = new byte[]{   this._transactionIdentifier[1],
                            this._transactionIdentifier[0],
                            this._protocolIdentifier[1],
                            this._protocolIdentifier[0],
                            this._length[1],
                            this._length[0],
                            this._unitIdentifier,
                            this._functionCode,
                            this._startingAddress[1],
                            this._startingAddress[0],
                            this._quantity[1],
                            this._quantity[0],
                            this._crc![0],
                            this._crc[1]
            };
            _crc = BitConverter.GetBytes(calculateCRC(data, 6, 6));
            data[12] = _crc[0];
            data[13] = _crc[1];
            if (_serialPort != null) {
                dataReceived = false;
                bytesToRead = 5 + 2 * quantity;


                //               serialport.ReceivedBytesThreshold = bytesToRead;
                _serialPort.Write(data, 6, 8);

                if (SendDataChanged != null) {
                    _sendData = new byte[8];
                    Array.Copy(data, 6, _sendData, 0, 8);
                    SendDataChanged(this);

                }
                data = new byte[2100];
                readBuffer = new byte[256];
                DateTime dateTimeSend = DateTime.Now;
                byte receivedUnitIdentifier = 0xFF;

                while (receivedUnitIdentifier != this._unitIdentifier & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout)) {
                    while (dataReceived == false & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout))
                        System.Threading.Thread.Sleep(1);
                    data = new byte[2100];
                    Array.Copy(readBuffer, 0, data, 6, readBuffer.Length);
                    receivedUnitIdentifier = data[6];
                }

                if (receivedUnitIdentifier != this._unitIdentifier)
                    data = new byte[2100];
                else
                    _countRetries = 0;
            }
            else if ((_tcpClient?.Client?.Connected ?? false) | _udpFlag) {
                if (_udpFlag) {

                    UdpClient udpClient = new UdpClient();
                    IPEndPoint endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress == null ? string.Empty : _ipAddress), _port);
                    udpClient.Send(data, data.Length - 2, endPoint);
                    _portOut = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;
                    udpClient.Client.ReceiveTimeout = 5000;
                    endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress == null ? string.Empty : _ipAddress), _portOut);
                    data = udpClient.Receive(ref endPoint);
                }
                else {
                    if (stream == null) {
                        throw (new Exception("tcp stream is not defined."));
                    }   

                    stream.Write(data, 0, data.Length - 2);

                    if (SendDataChanged != null) {

                        _sendData = new byte[data.Length - 2];
                        Array.Copy(data, 0, _sendData, 0, data.Length - 2);
                        SendDataChanged(this);
                    }
                    data = new Byte[2100];
                    int NumberOfBytes = stream.Read(data, 0, data.Length);
                    if (ReceiveDataChanged != null) {

                        _receiveData = new byte[NumberOfBytes];
                        Array.Copy(data, 0, _receiveData, 0, NumberOfBytes);
                        
                        ReceiveDataChanged(this);
                    }

                }
            }
            if (data[7] == 0x84 & data[8] == 0x01) {
                
                LastError = "Function code not supported by master";
                return false;
            }
            if (data[7] == 0x84 & data[8] == 0x02) {

                LastError = "Starting address invalid or starting address + quantity invalid";
                return false;
            }
            if (data[7] == 0x84 & data[8] == 0x03) {

                LastError = "quantity invalid";
                return false;
            }
            if (data[7] == 0x84 & data[8] == 0x04) {
                
                LastError = "error reading";
                return false;
            }
            if (_serialPort != null) {

                _crc = BitConverter.GetBytes(calculateCRC(data, (ushort)(data[8] + 3), 6));
                
                if ((_crc[0] != data[data[8] + 9] | _crc[1] != data[data[8] + 10]) & dataReceived) {
                    
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        LastError = "Response CRC check failed";
                        return false;
                    }
                    else {
                        _countRetries++;
                        return ReadInputRegisters(startingAddress, quantity, out response, functionCode);
                    }
                }
                else if (!dataReceived) {
                    
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        LastError = "No Response from Modbus Slave";
                        return false;
                    }
                    else {
                        _countRetries++;
                        return ReadInputRegisters(startingAddress, quantity, out response,functionCode);
                    }

                }
            }
            response = new int[quantity];
            for (int i = 0; i < quantity; i++) {
                byte lowByte;
                byte highByte;
                highByte = data[9 + i * 2];
                lowByte = data[9 + i * 2 + 1];

                data[9 + i * 2] = lowByte;
                data[9 + i * 2 + 1] = highByte;

                response[i] = BitConverter.ToInt16(data, (9 + i * 2));
            }

            return true;
        }



        /// <summary>
        /// Write single Coil to Master device (FC5).
        /// </summary>
        /// <param name="startingAddress">Coil to be written</param>
        /// <param name="value">Coil Value to be written</param>
        /// <param name="functionCode">Function code to be used in the ModBusTransaction. Optional. Set to 0x05 by deafult.</param>
        public void WriteSingleCoil(int startingAddress, bool value, int functionCode = 0x05)
        {

            // if (debug) StoreLogData.Instance.Store("FC5 (Write single coil to Master device), StartingAddress: "+ startingAddress+", Value: " + value, System.DateTime.Now);
            _transactionIdentifierInternal++;
            if (_serialPort != null) {

                if (!_serialPort.IsOpen) {
                    // if (debug) StoreLogData.Instance.Store("SerialPortNotOpenedException Throwed", System.DateTime.Now);
                    throw new ModeBusHandler.Exceptions.SerialPortNotOpenedException("serial port not opened");
                }
            }
            if (_tcpClient == null & !_udpFlag & _serialPort == null) {
                // if (debug) StoreLogData.Instance.Store("ConnectionException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.ConnectionException("connection error");
            }

            byte[] coilValue = new byte[2];
            this._transactionIdentifier = BitConverter.GetBytes((uint)_transactionIdentifierInternal);
            this._protocolIdentifier = BitConverter.GetBytes((int)0x0000);
            this._length = BitConverter.GetBytes((int)0x0006);
            this._functionCode = (byte) functionCode;
            this._startingAddress = BitConverter.GetBytes(startingAddress);

            if (value == true) {
                coilValue = BitConverter.GetBytes((int)0xFF00);
            }
            else {
                coilValue = BitConverter.GetBytes((int)0x0000);
            }

            Byte[] data = new byte[]{   this._transactionIdentifier[1],
                            this._transactionIdentifier[0],
                            this._protocolIdentifier[1],
                            this._protocolIdentifier[0],
                            this._length[1],
                            this._length[0],
                            this._unitIdentifier,
                            this._functionCode,
                            this._startingAddress[1],
                            this._startingAddress[0],
                            coilValue[1],
                            coilValue[0],
                            this._crc![0],
                            this._crc[1]
                            };
            _crc = BitConverter.GetBytes(calculateCRC(data, 6, 6));
            data[12] = _crc[0];
            data[13] = _crc[1];

            if (_serialPort != null) {

                dataReceived = false;
                bytesToRead = 8;
                //               serialport.ReceivedBytesThreshold = bytesToRead;
                _serialPort.Write(data, 6, 8);

                if (SendDataChanged != null) {
                    _sendData = new byte[8];
                    Array.Copy(data, 6, _sendData, 0, 8);
                    SendDataChanged(this);

                }

                data = new byte[2100];
                readBuffer = new byte[256];
                DateTime dateTimeSend = DateTime.Now;
                byte receivedUnitIdentifier = 0xFF;

                while (receivedUnitIdentifier != this._unitIdentifier & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout)) {
                    while (dataReceived == false & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout))
                        System.Threading.Thread.Sleep(1);
                    data = new byte[2100];
                    Array.Copy(readBuffer, 0, data, 6, readBuffer.Length);
                    receivedUnitIdentifier = data[6];
                }

                if (receivedUnitIdentifier != this._unitIdentifier)
                    data = new byte[2100];
                else
                    _countRetries = 0;

            }
            else if ((_tcpClient?.Client?.Connected ?? false) | _udpFlag) {

                if (_udpFlag) {

                    UdpClient udpClient = new UdpClient();
                    IPEndPoint endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress!), _port);
                    udpClient.Send(data, data.Length - 2, endPoint);
                    _portOut = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;
                    udpClient.Client.ReceiveTimeout = 5000;
                    endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress!), _portOut);
                    data = udpClient.Receive(ref endPoint);
                }
                else {

                    stream!.Write(data, 0, data.Length - 2);

                    if (SendDataChanged != null) {
                        _sendData = new byte[data.Length - 2];
                        Array.Copy(data, 0, _sendData, 0, data.Length - 2);
                        SendDataChanged(this);

                    }
                    data = new Byte[2100];
                    int NumberOfBytes = stream.Read(data, 0, data.Length);
                    if (ReceiveDataChanged != null) {
                        _receiveData = new byte[NumberOfBytes];
                        Array.Copy(data, 0, _receiveData, 0, NumberOfBytes);
                        // if (debug) StoreLogData.Instance.Store("Receive ModbusTCP-Data: " + BitConverter.ToString(receiveData), System.DateTime.Now);
                        ReceiveDataChanged(this);
                    }
                }
            }

            if (data[7] == 0x85 & data[8] == 0x01) {
                // if (debug) StoreLogData.Instance.Store("FunctionCodeNotSupportedException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.FunctionCodeNotSupportedException("Function code not supported by master");
            }

            if (data[7] == 0x85 & data[8] == 0x02) {
                // if (debug) StoreLogData.Instance.Store("StartingAddressInvalidException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.StartingAddressInvalidException("Starting address invalid or starting address + quantity invalid");
            }

            if (data[7] == 0x85 & data[8] == 0x03) {
                // if (debug) StoreLogData.Instance.Store("QuantityInvalidException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.QuantityInvalidException("quantity invalid");
            }

            if (data[7] == 0x85 & data[8] == 0x04) {
                // if (debug) StoreLogData.Instance.Store("ModbusException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.ModbusException("error reading");
            }

            if (_serialPort != null) {

                _crc = BitConverter.GetBytes(calculateCRC(data, 6, 6));

                if ((_crc[0] != data[12] | _crc[1] != data[13]) & dataReceived) {
                    // if (debug) StoreLogData.Instance.Store("CRCCheckFailedException Throwed", System.DateTime.Now);
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        throw new ModeBusHandler.Exceptions.CRCCheckFailedException("Response CRC check failed");
                    }
                    else {
                        _countRetries++;
                        WriteSingleCoil(startingAddress, value, functionCode);
                    }
                }
                else if (!dataReceived) {
                    // if (debug) StoreLogData.Instance.Store("TimeoutException Throwed", System.DateTime.Now);
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        throw new TimeoutException("No Response from Modbus Slave");

                    }
                    else {
                        _countRetries++;
                        WriteSingleCoil(startingAddress, value, functionCode);
                    }
                }
            }
        }



        /// <summary>
        /// Write single Register to Master device (FC6).
        /// </summary>
        /// <param name="startingAddress">Register to be written</param>
        /// <param name="value">Register Value to be written</param>
        /// <param name="functionCode">Function code to be used in the ModBusTransaction. Optional. Set to 0x06 by deafult.</param>
        public bool WriteSingle16bitRegister(int startingAddress, ushort value, int functionCode = 0x06)
        {
            // if (debug) StoreLogData.Instance.Store("FC6 (Write single register to Master device), StartingAddress: "+ startingAddress+", Value: " + value, System.DateTime.Now);
            _transactionIdentifierInternal++;

            if (_serialPort != null) {
                if (!_serialPort.IsOpen) {
                    LastError = "serial port not opened";
                    return false;
                }
            }

            if (_tcpClient == null & !_udpFlag & _serialPort == null) {
                LastError = "connection error";
                return false;
            }

            byte[] registerValue = new byte[2];
            this._transactionIdentifier = 
                BitConverter.GetBytes((uint)_transactionIdentifierInternal);
            this._protocolIdentifier = 
                BitConverter.GetBytes((int)0x0000);
            this._length = 
                BitConverter.GetBytes((int)0x0006);
            this._functionCode = (byte) functionCode;
            this._startingAddress = 
                BitConverter.GetBytes(startingAddress);
            registerValue = 
                BitConverter.GetBytes(value);

            Byte[] data = new byte[]{   this._transactionIdentifier[1],
                            this._transactionIdentifier[0],
                            this._protocolIdentifier[1],
                            this._protocolIdentifier[0],
                            this._length[1],
                            this._length[0],
                            this._unitIdentifier,
                            this._functionCode,
                            this._startingAddress[1],
                            this._startingAddress[0],
                            registerValue[1],
                            registerValue[0],
                            this._crc![0],
                            this._crc[1]
            };

            _crc = BitConverter.GetBytes(calculateCRC(data, 6, 6));
            data[12] = _crc[0];
            data[13] = _crc[1];

            if (_serialPort != null) {

                dataReceived = false;
                bytesToRead = 8;
                //                serialport.ReceivedBytesThreshold = bytesToRead;
                _serialPort.Write(data, 6, 8);

                if (SendDataChanged != null) {
                    _sendData = new byte[8];
                    Array.Copy(data, 6, _sendData, 0, 8);
                    SendDataChanged(this);
                }

                data = new byte[2100];
                readBuffer = new byte[256];
                DateTime dateTimeSend = DateTime.Now;
                byte receivedUnitIdentifier = 0xFF;

                while (receivedUnitIdentifier != this._unitIdentifier 
                      & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > 
                           TimeSpan.TicksPerMillisecond * this._connectTimeout)) {

                    while (dataReceived == false 
                           & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > 
                                TimeSpan.TicksPerMillisecond * this._connectTimeout)) {
                        System.Threading.Thread.Sleep(1);
                    }

                    data = new byte[2100];
                    Array.Copy(readBuffer, 0, data, 6, readBuffer.Length);
                    receivedUnitIdentifier = data[6];
                }

                if (receivedUnitIdentifier != this._unitIdentifier) {
                    data = new byte[2100];
                }
                else {
                    _countRetries = 0;
                }
            }
            else if ((_tcpClient?.Client?.Connected ?? false) | _udpFlag) {

                if (_udpFlag) {

                    UdpClient udpClient = new UdpClient();
                    IPEndPoint endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress!), _port);
                    udpClient.Send(data, data.Length - 2, endPoint);
                    _portOut = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;
                    udpClient.Client.ReceiveTimeout = 5000;
                    endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress!), _portOut);
                    data = udpClient.Receive(ref endPoint);
                }
                else {
                    stream!.Write(data, 0, data.Length - 2);

                    if (SendDataChanged != null) {
                        _sendData = new byte[data.Length - 2];
                        Array.Copy(data, 0, _sendData, 0, data.Length - 2);
                        SendDataChanged(this);
                    }

                    data = new Byte[2100];
                    int NumberOfBytes = stream.Read(data, 0, data.Length);

                    if (ReceiveDataChanged != null) {
                        _receiveData = new byte[NumberOfBytes];
                        Array.Copy(data, 0, _receiveData, 0, NumberOfBytes);
                        ReceiveDataChanged(this);
                    }
                }
            }

            if ( !DataIsCosher(data) ) {
                return false;
            }

            if (_serialPort != null) {

                _crc = BitConverter.GetBytes(calculateCRC(data, 6, 6));

                if ((_crc[0] != data[12] | _crc[1] != data[13]) & dataReceived) {
                    // if (debug) StoreLogData.Instance.Store("CRCCheckFailedException Throwed", System.DateTime.Now);
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        LastError = "Response CRC check failed";
                        return false;
                    }
                    else {
                        _countRetries++;
                        WriteSingle32bitRegister(startingAddress, value);
                    }
                }
                else if (!dataReceived) {
                    // if (debug) StoreLogData.Instance.Store("TimeoutException Throwed", System.DateTime.Now);
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        LastError = "No Response from Modbus Slave";
                        return false;
                    }
                    else {
                        _countRetries++;
                        WriteSingle32bitRegister(startingAddress, value);
                    }
                }
            }
            return true;
        }


        internal bool DataIsCosher(byte[] data, bool useExceptions = false)
        {
            if (data[7] == 0x86) {
                switch (data[8]) {
                    case 0x01:
                        LastError = "Function code not supported by master";
                        if (useExceptions) {
                            throw new Exception(LastError);
                        }
                        return false;

                    case 0x02:
                        LastError = "Starting address invalid or " +
                            "starting address + quantity invalid";
                        if (useExceptions) {
                            throw new Exception(LastError);
                        }
                        return false;

                    case 0x03:
                        LastError = "Quantity invalid.";
                        if (useExceptions) {
                            throw new Exception(LastError);
                        }
                        return false;

                    case 0x04:
                        LastError = "Error reading";
                        if (useExceptions) {
                            throw new Exception(LastError);
                        }
                        return false;

                    default : 
                        return true;
                }
            }
            return true;
        }


        /// <summary>
        /// Write single Register to Master device (FC6).
        /// </summary>
        /// <param name="startingAddress">Register to be written</param>
        /// <param name="value">Register Value to be written</param>
        /// <param name="functionCode">Function code to be used in the ModBusTransaction. Optional. Set to 0x06 by deafult.</param>
        public bool WriteSingle32bitRegister(int startingAddress, int value, int functionCode = 0x06)
        {
            // if (debug) StoreLogData.Instance.Store("FC6 (Write single register to Master device), StartingAddress: "+ startingAddress+", Value: " + value, System.DateTime.Now);
            _transactionIdentifierInternal++;

            if (_serialPort != null) {

                if (!_serialPort.IsOpen) {

                    LastError = "serial port not opened";
                    return false;
                }
            }

            if (_tcpClient == null & !_udpFlag & _serialPort == null) {
                
                LastError = "connection error";
                return false;
            }

            byte[] registerValue = new byte[2];
            this._transactionIdentifier = BitConverter.GetBytes((uint)_transactionIdentifierInternal);
            this._protocolIdentifier = BitConverter.GetBytes((int)0x0000);
            this._length = BitConverter.GetBytes((int)0x0006);
            this._functionCode = (byte)functionCode;
            this._startingAddress = BitConverter.GetBytes(startingAddress);
            registerValue = BitConverter.GetBytes((int)value);

            Byte[] data = new byte[]{   this._transactionIdentifier[1],
                            this._transactionIdentifier[0],
                            this._protocolIdentifier[1],
                            this._protocolIdentifier[0],
                            this._length[1],
                            this._length[0],
                            this._unitIdentifier,
                            this._functionCode,
                            this._startingAddress[1],
                            this._startingAddress[0],
                            registerValue[1],
                            registerValue[0],
                            this._crc![0],
                            this._crc[1]
            };

            _crc = BitConverter.GetBytes(calculateCRC(data, 6, 6));
            data[12] = _crc[0];
            data[13] = _crc[1];

            if (_serialPort != null) {

                dataReceived = false;
                bytesToRead = 8;
                //                serialport.ReceivedBytesThreshold = bytesToRead;
                _serialPort.Write(data, 6, 8);

                if (SendDataChanged != null) {

                    _sendData = new byte[8];
                    Array.Copy(data, 6, _sendData, 0, 8);
                    SendDataChanged(this);

                }
                data = new byte[2100];
                readBuffer = new byte[256];
                DateTime dateTimeSend = DateTime.Now;
                byte receivedUnitIdentifier = 0xFF;

                while ( receivedUnitIdentifier != this._unitIdentifier & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout)) {
                    
                    while (dataReceived == false & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout)) { 
                    
                        System.Threading.Thread.Sleep(1); 
                    }
              
                    data = new byte[2100];
                    Array.Copy(readBuffer, 0, data, 6, readBuffer.Length);
                    receivedUnitIdentifier = data[6];
                }

                if (receivedUnitIdentifier != this._unitIdentifier) {

                    data = new byte[2100];
                }
                else {
                    _countRetries = 0;
                }
            }
            else if ((_tcpClient?.Client?.Connected ?? false) | _udpFlag) {
                
                if (_udpFlag) {
                
                    UdpClient udpClient = new UdpClient();
                    IPEndPoint endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress!), _port);
                    udpClient.Send(data, data.Length - 2, endPoint);
                    _portOut = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;
                    udpClient.Client.ReceiveTimeout = 5000;
                    endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress!), _portOut);
                    data = udpClient.Receive(ref endPoint);
                }
                else {

                    stream!.Write(data, 0, data.Length - 2);

                    if (SendDataChanged != null) {
                    
                        _sendData = new byte[data.Length - 2];
                        Array.Copy(data, 0, _sendData, 0, data.Length - 2);
                        SendDataChanged(this);
                    }

                    data = new Byte[2100];
                    int NumberOfBytes = stream.Read(data, 0, data.Length);
                    
                    if (ReceiveDataChanged != null) {
                        
                        _receiveData = new byte[NumberOfBytes];
                        Array.Copy(data, 0, _receiveData, 0, NumberOfBytes);
                        ReceiveDataChanged(this);
                    }
                }
            }

            if (data[7] == 0x86 & data[8] == 0x01) {
                LastError = "Function code not supported by master";
                return false;
            }

            if (data[7] == 0x86 & data[8] == 0x02) {
                LastError = "Starting address invalid or starting address + quantity invalid";
                return false;
            }

            if (data[7] == 0x86 & data[8] == 0x03) {
                LastError = "quantity invalid";
            }
            if (data[7] == 0x86 & data[8] == 0x04) {
                LastError = "error reading";
                return false;
            }
            if (_serialPort != null) {

                _crc = BitConverter.GetBytes(calculateCRC(data, 6, 6));
               
                if ((_crc[0] != data[12] | _crc[1] != data[13]) & dataReceived) {
                    // if (debug) StoreLogData.Instance.Store("CRCCheckFailedException Throwed", System.DateTime.Now);
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        LastError = "Response CRC check failed";
                        return false;
                    }
                    else {
                        _countRetries++;
                        WriteSingle32bitRegister(startingAddress, value);
                    }
                }
                else if (!dataReceived) {
                    // if (debug) StoreLogData.Instance.Store("TimeoutException Throwed", System.DateTime.Now);
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        LastError = "No Response from Modbus Slave";
                        return false;
                    }
                    else {
                        _countRetries++;
                        WriteSingle32bitRegister(startingAddress, value);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Write multiple coils to Master device (FC15).
        /// </summary>
        /// <param name="startingAddress">First coil to be written</param>
        /// <param name="values">Coil Values to be written</param>
        /// <param name="functionCode">Function code to be used in the ModBusTransaction. Optional. Set to 0x0F by deafult.</param>
        public bool WriteMultipleCoils(int startingAddress, bool[] values, int functionCode = 0x0F)
        {
            string debugString = "";
            for (int i = 0; i < values.Length; i++)
                debugString = debugString + values[i] + " ";
            // if (debug) StoreLogData.Instance.Store("FC15 (Write multiple coils to Master device), StartingAddress: "+ startingAddress+", Values: " + debugString, System.DateTime.Now);
            _transactionIdentifierInternal++;
            byte byteCount = (byte)((values.Length % 8 != 0 ? values.Length / 8 + 1: (values.Length / 8)));
            byte[] quantityOfOutputs = BitConverter.GetBytes((int)values.Length);
            byte singleCoilValue = 0;
            if (_serialPort != null) {
             
                if (!_serialPort.IsOpen) {
                   
                    LastError = "serial port not opened";
                    return false;
                }
            }
            if (_tcpClient == null & !_udpFlag & _serialPort == null) {
                LastError = "connection error";
                return false;
            }
            this._transactionIdentifier = BitConverter.GetBytes((uint)_transactionIdentifierInternal);
            this._protocolIdentifier = BitConverter.GetBytes((int)0x0000);
            this._length = BitConverter.GetBytes((int)(7 + (byteCount)));
            this._functionCode = (byte)functionCode;
            this._startingAddress = BitConverter.GetBytes(startingAddress);



            Byte[] data = new byte[14 +2 + (values.Length % 8 != 0 ? values.Length/8 : (values.Length / 8)-1)];
            data[0] = this._transactionIdentifier[1];
            data[1] = this._transactionIdentifier[0];
            data[2] = this._protocolIdentifier[1];
            data[3] = this._protocolIdentifier[0];
            data[4] = this._length[1];
            data[5] = this._length[0];
            data[6] = this._unitIdentifier;
            data[7] = this._functionCode;
            data[8] = this._startingAddress[1];
            data[9] = this._startingAddress[0];
            data[10] = quantityOfOutputs[1];
            data[11] = quantityOfOutputs[0];
            data[12] = byteCount;

            for (int i = 0; i < values.Length; i++) {

                if ((i % 8) == 0) {
                    singleCoilValue = 0;
                }
                byte CoilValue;
                if (values[i] == true) {
                    CoilValue = 1;
                }
                else {
                    CoilValue = 0;
                }

                singleCoilValue = (byte)((int)CoilValue << (i % 8) | (int)singleCoilValue);

                data[13 + (i / 8)] = singleCoilValue;
            }

            _crc = BitConverter.GetBytes(calculateCRC(data, (ushort)(data.Length - 8), 6));
            data[data.Length - 2] = _crc[0];
            data[data.Length - 1] = _crc[1];

            if (_serialPort != null) {
            
                dataReceived = false;
                bytesToRead = 8;
                //               serialport.ReceivedBytesThreshold = bytesToRead;
                _serialPort.Write(data, 6, data.Length - 6);

                if (SendDataChanged != null) {
            
                    _sendData = new byte[data.Length - 6];
                    Array.Copy(data, 6, _sendData, 0, data.Length - 6);
                    SendDataChanged(this);
                }

                data = new byte[2100];
                readBuffer = new byte[256];
                DateTime dateTimeSend = DateTime.Now;
                byte receivedUnitIdentifier = 0xFF;
                
                while (receivedUnitIdentifier != this._unitIdentifier & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout)) {
                    while (dataReceived == false & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout)) {
                        System.Threading.Thread.Sleep(1);
                    }
                    data = new byte[2100];
                    Array.Copy(readBuffer, 0, data, 6, readBuffer.Length);
                    receivedUnitIdentifier = data[6];
                }
                if (receivedUnitIdentifier != this._unitIdentifier)
                    data = new byte[2100];
                else
                    _countRetries = 0;
            }
            else if ((_tcpClient?.Client?.Connected ?? false) | _udpFlag) {
                
                if (_udpFlag) {
                
                    UdpClient udpClient = new UdpClient();
                    IPEndPoint endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress!), _port);
                    udpClient.Send(data, data.Length - 2, endPoint);
                    _portOut = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;
                    udpClient.Client.ReceiveTimeout = 5000;
                    endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress!), _portOut);
                    data = udpClient.Receive(ref endPoint);
                }
                else {
                    stream!.Write(data, 0, data.Length - 2);

                    if (SendDataChanged != null) {
                        _sendData = new byte[data.Length - 2];
                        Array.Copy(data, 0, _sendData, 0, data.Length - 2);
                        SendDataChanged(this);
                    }
                    data = new Byte[2100];
                    int NumberOfBytes = stream.Read(data, 0, data.Length);
                    
                    if (ReceiveDataChanged != null) {
                        _receiveData = new byte[NumberOfBytes];
                        Array.Copy(data, 0, _receiveData, 0, NumberOfBytes);
                        ReceiveDataChanged(this);
                    }
                }
            }
            if (data[7] == 0x8F & data[8] == 0x01) {
                LastError = "Function code not supported by master";
                return false;
            }
            if (data[7] == 0x8F & data[8] == 0x02) {
                LastError = "Starting address invalid or starting address + quantity invalid";
                return false;
            }
            if (data[7] == 0x8F & data[8] == 0x03) {
                LastError = "quantity invalid";
                return false;
            }
            if (data[7] == 0x8F & data[8] == 0x04) {
                LastError = "error reading";
                return false;
            }
            if (_serialPort != null) {

                _crc = BitConverter.GetBytes(calculateCRC(data, 6, 6));

                if ((_crc[0] != data[12] | _crc[1] != data[13]) & dataReceived) {

                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        LastError = "Response CRC check failed";
                        return false;
                    }
                    else {
                        _countRetries++;
                        WriteMultipleCoils(startingAddress, values);
                    }
                }
                else if (!dataReceived) {
                    
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        LastError = "No Response from Modbus Slave";
                        return false;
                    }
                    else {
                        _countRetries++;
                        WriteMultipleCoils(startingAddress, values);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Write multiple registers to Master device (FC16).
        /// </summary>
        /// <param name="startingAddress">First register to be written</param>
        /// <param name="values">register Values to be written</param>
        /// <param name="functionCode">Function code to be used in the ModBusTransaction. Optional. Set to 0x10 by deafult.</param>
        public void WriteMultipleRegisters(int startingAddress, int[] values, int functionCode = 0x10)
        {
            string debugString = "";
            for (int i = 0; i < values.Length; i++)
                debugString = debugString + values[i] + " ";
            // if (debug) StoreLogData.Instance.Store("FC16 (Write multiple Registers to Server device), StartingAddress: "+ startingAddress+", Values: " + debugString, System.DateTime.Now);
            _transactionIdentifierInternal++;
            byte byteCount = (byte)(values.Length * 2);
            byte[] quantityOfOutputs = BitConverter.GetBytes((int)values.Length);
            if (_serialPort != null)
                if (!_serialPort.IsOpen) {
                    // if (debug) StoreLogData.Instance.Store("SerialPortNotOpenedException Throwed", System.DateTime.Now);
                    throw new ModeBusHandler.Exceptions.SerialPortNotOpenedException("serial port not opened");
                }
            if (_tcpClient == null & !_udpFlag & _serialPort == null) {
                // if (debug) StoreLogData.Instance.Store("ConnectionException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.ConnectionException("connection error");
            }
            this._transactionIdentifier = BitConverter.GetBytes((uint)_transactionIdentifierInternal);
            this._protocolIdentifier = BitConverter.GetBytes((int)0x0000);
            this._length = BitConverter.GetBytes((int)(7 + values.Length * 2));
            this._functionCode = (byte) functionCode;
            this._startingAddress = BitConverter.GetBytes(startingAddress);

            Byte[] data = new byte[13+2 + values.Length*2];
            data[0] = this._transactionIdentifier[1];
            data[1] = this._transactionIdentifier[0];
            data[2] = this._protocolIdentifier[1];
            data[3] = this._protocolIdentifier[0];
            data[4] = this._length[1];
            data[5] = this._length[0];
            data[6] = this._unitIdentifier;
            data[7] = this._functionCode;
            data[8] = this._startingAddress[1];
            data[9] = this._startingAddress[0];
            data[10] = quantityOfOutputs[1];
            data[11] = quantityOfOutputs[0];
            data[12] = byteCount;
            for (int i = 0; i < values.Length; i++) {
                byte[] singleRegisterValue = BitConverter.GetBytes((int)values[i]);
                data[13 + i * 2] = singleRegisterValue[1];
                data[14 + i * 2] = singleRegisterValue[0];
            }
            _crc = BitConverter.GetBytes(calculateCRC(data, (ushort)(data.Length - 8), 6));
            data[data.Length - 2] = _crc[0];
            data[data.Length - 1] = _crc[1];

            if (_serialPort != null) {

                dataReceived = false;
                bytesToRead = 8;

                //                serialport.ReceivedBytesThreshold = bytesToRead;
                _serialPort.Write(data, 6, data.Length - 6);

                if (SendDataChanged != null) {

                    _sendData = new byte[data.Length - 6];
                    Array.Copy(data, 6, _sendData, 0, data.Length - 6);
                    SendDataChanged(this);
                }

                data = new byte[2100];
                readBuffer = new byte[256];
                DateTime dateTimeSend = DateTime.Now;
                byte receivedUnitIdentifier = 0xFF;

                while (receivedUnitIdentifier != this._unitIdentifier 
                       & !((DateTime.Now.Ticks - dateTimeSend.Ticks)  
                           > TimeSpan.TicksPerMillisecond * this._connectTimeout)) {

                    while (dataReceived == false
                        & !((DateTime.Now.Ticks - dateTimeSend.Ticks)
                             > TimeSpan.TicksPerMillisecond * this._connectTimeout)) {
                     
                        System.Threading.Thread.Sleep(1);
                    }

                    data = new byte[2100];
                    Array.Copy(readBuffer, 0, data, 6, readBuffer.Length);
                    receivedUnitIdentifier = data[6];
                }

                if (receivedUnitIdentifier != this._unitIdentifier) {
                    data = new byte[2100];
                }
                else {
                    _countRetries = 0;
                }
            }
            else if ((_tcpClient?.Client?.Connected ?? false) | _udpFlag) {
               
                if (_udpFlag) {
                
                    UdpClient udpClient = new UdpClient();
                    IPEndPoint endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress!), _port);
                    udpClient.Send(data, data.Length - 2, endPoint);
                    _portOut = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;
                    udpClient.Client.ReceiveTimeout = 5000;
                    endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress!), _portOut);
                    data = udpClient.Receive(ref endPoint);
                }
                else {
                    stream!.Write(data, 0, data.Length - 2);

                    if (SendDataChanged != null) {
                        _sendData = new byte[data.Length - 2];
                        Array.Copy(data, 0, _sendData, 0, data.Length - 2);
                        SendDataChanged(this);
                    }

                    data = new Byte[2100];
                    int NumberOfBytes = stream.Read(data, 0, data.Length);
                    
                    if (ReceiveDataChanged != null) {
                        _receiveData = new byte[NumberOfBytes];
                        Array.Copy(data, 0, _receiveData, 0, NumberOfBytes);
                        // if (debug) StoreLogData.Instance.Store("Receive ModbusTCP-Data: " + BitConverter.ToString(receiveData), System.DateTime.Now);
                        ReceiveDataChanged(this);
                    }
                }
            }

            if (data[7] == 0x90 & data[8] == 0x01) {
                // if (debug) StoreLogData.Instance.Store("FunctionCodeNotSupportedException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.FunctionCodeNotSupportedException("Function code not supported by master");
            }
            if (data[7] == 0x90 & data[8] == 0x02) {
                // if (debug) StoreLogData.Instance.Store("StartingAddressInvalidException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.StartingAddressInvalidException("Starting address invalid or starting address + quantity invalid");
            }
            if (data[7] == 0x90 & data[8] == 0x03) {
                // if (debug) StoreLogData.Instance.Store("QuantityInvalidException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.QuantityInvalidException("quantity invalid");
            }
            if (data[7] == 0x90 & data[8] == 0x04) {
                // if (debug) StoreLogData.Instance.Store("ModbusException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.ModbusException("error reading");
            }
            if (_serialPort != null) {
                _crc = BitConverter.GetBytes(calculateCRC(data, 6, 6));
                if ((_crc[0] != data[12] | _crc[1] != data[13]) & dataReceived) {
                    // if (debug) StoreLogData.Instance.Store("CRCCheckFailedException Throwed", System.DateTime.Now);
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        throw new ModeBusHandler.Exceptions.CRCCheckFailedException("Response CRC check failed");
                    }
                    else {
                        _countRetries++;
                        WriteMultipleRegisters(startingAddress, values);
                    }
                }
                else if (!dataReceived) {
                    // if (debug) StoreLogData.Instance.Store("TimeoutException Throwed", System.DateTime.Now);
                    if (MaxNumberOfRetries <= _countRetries) {
                        _countRetries = 0;
                        throw new TimeoutException("No Response from Modbus Slave");

                    }
                    else {
                        _countRetries++;
                        WriteMultipleRegisters(startingAddress, values);
                    }
                }
            }
        }

        /// <summary>
        /// Read/Write Multiple Registers (FC23).
        /// </summary>
        /// <param name="startingAddressRead">First input register to read</param>
        /// <param name="quantityRead">Number of input registers to read</param>
        /// <param name="startingAddressWrite">First input register to write</param>
        /// <param name="values">Values to write</param>
        /// <param name="functionCode">Function code to be used in the ModBusTransaction. Optional. Set to 0x17 by deafult.</param>
        /// <returns>Int Array which contains the Holding registers</returns>
        public int[] ReadWriteMultipleRegisters(int startingAddressRead, int quantityRead, 
            int startingAddressWrite, int[] values, int functionCode = 0x17)
        {

            string debugString = "";
            for (int i = 0; i < values.Length; i++)
                debugString = debugString + values[i] + " ";
            // if (debug) StoreLogData.Instance.Store("FC23 (Read and Write multiple Registers to Server device), StartingAddress Read: "+ startingAddressRead+ ", Quantity Read: "+quantityRead+", startingAddressWrite: " + startingAddressWrite +", Values: " + debugString, System.DateTime.Now);
            _transactionIdentifierInternal++;
            byte [] startingAddressReadLocal = new byte[2];
            byte [] quantityReadLocal = new byte[2];
            byte[] startingAddressWriteLocal = new byte[2];
            byte[] quantityWriteLocal = new byte[2];
            byte writeByteCountLocal = 0;
            if (_serialPort != null)
                if (!_serialPort.IsOpen) {
                    // if (debug) StoreLogData.Instance.Store("SerialPortNotOpenedException Throwed", System.DateTime.Now);
                    throw new ModeBusHandler.Exceptions.SerialPortNotOpenedException("serial port not opened");
                }
            if (_tcpClient == null & !_udpFlag & _serialPort == null) {
                // if (debug) StoreLogData.Instance.Store("ConnectionException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.ConnectionException("connection error");
            }
            if (startingAddressRead > 65535 | quantityRead > 125 | startingAddressWrite > 65535 | values.Length > 121) {
                // if (debug) StoreLogData.Instance.Store("ArgumentException Throwed", System.DateTime.Now);
                throw new ArgumentException("Starting address must be 0 - 65535; quantity must be 0 - 2000");
            }
            int[] response;
            this._transactionIdentifier = BitConverter.GetBytes((uint)_transactionIdentifierInternal);
            this._protocolIdentifier = BitConverter.GetBytes((int)0x0000);
            this._length = BitConverter.GetBytes((int)11 + values.Length * 2);
            this._functionCode = (byte) functionCode;
            startingAddressReadLocal = BitConverter.GetBytes(startingAddressRead);
            quantityReadLocal = BitConverter.GetBytes(quantityRead);
            startingAddressWriteLocal = BitConverter.GetBytes(startingAddressWrite);
            quantityWriteLocal = BitConverter.GetBytes(values.Length);
            writeByteCountLocal = Convert.ToByte(values.Length * 2);
            Byte[] data = new byte[17 +2+ values.Length*2];
            data[0] = this._transactionIdentifier[1];
            data[1] = this._transactionIdentifier[0];
            data[2] = this._protocolIdentifier[1];
            data[3] = this._protocolIdentifier[0];
            data[4] = this._length[1];
            data[5] = this._length[0];
            data[6] = this._unitIdentifier;
            data[7] = this._functionCode;
            data[8] = startingAddressReadLocal[1];
            data[9] = startingAddressReadLocal[0];
            data[10] = quantityReadLocal[1];
            data[11] = quantityReadLocal[0];
            data[12] = startingAddressWriteLocal[1];
            data[13] = startingAddressWriteLocal[0];
            data[14] = quantityWriteLocal[1];
            data[15] = quantityWriteLocal[0];
            data[16] = writeByteCountLocal;

            for (int i = 0; i < values.Length; i++) {
                byte[] singleRegisterValue = BitConverter.GetBytes((int)values[i]);
                data[17 + i * 2] = singleRegisterValue[1];
                data[18 + i * 2] = singleRegisterValue[0];
            }
            _crc = BitConverter.GetBytes(calculateCRC(data, (ushort)(data.Length - 8), 6));
            data[data.Length - 2] = _crc[0];
            data[data.Length - 1] = _crc[1];
            if (_serialPort != null) {
                dataReceived = false;
                bytesToRead = 5 + 2 * quantityRead;
                //               serialport.ReceivedBytesThreshold = bytesToRead;
                _serialPort.Write(data, 6, data.Length - 6);

                if (SendDataChanged != null) {
                    _sendData = new byte[data.Length - 6];
                    Array.Copy(data, 6, _sendData, 0, data.Length - 6);
                    SendDataChanged(this);
                }
                data = new byte[2100];
                readBuffer = new byte[256];
                DateTime dateTimeSend = DateTime.Now;
                byte receivedUnitIdentifier = 0xFF;
                while (receivedUnitIdentifier != this._unitIdentifier & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout)) {
                    while (dataReceived == false & !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * this._connectTimeout))
                        System.Threading.Thread.Sleep(1);
                    data = new byte[2100];
                    Array.Copy(readBuffer, 0, data, 6, readBuffer.Length);
                    receivedUnitIdentifier = data[6];
                }
                if (receivedUnitIdentifier != this._unitIdentifier)
                    data = new byte[2100];
                else
                    _countRetries = 0;
            }
            else if ((_tcpClient?.Client?.Connected ?? false) | _udpFlag) {
                if (_udpFlag) {
                    UdpClient udpClient = new UdpClient();
                    IPEndPoint endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress == null ? string.Empty : _ipAddress), _port);
                    udpClient.Send(data, data.Length - 2, endPoint);
                    _portOut = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;
                    udpClient.Client.ReceiveTimeout = 5000;
                    endPoint = new IPEndPoint(System.Net.IPAddress.Parse(_ipAddress == null ? string.Empty : _ipAddress), _portOut);
                    data = udpClient.Receive(ref endPoint);
                }
                else {
                    stream!.Write(data, 0, data.Length - 2);


                    if (SendDataChanged != null) {
                        _sendData = new byte[data.Length - 2];
                        Array.Copy(data, 0, _sendData, 0, data.Length - 2);
                        SendDataChanged(this);

                    }
                    data = new Byte[2100];
                    int NumberOfBytes = stream.Read(data, 0, data.Length);
                    if (ReceiveDataChanged != null) {
                        _receiveData = new byte[NumberOfBytes];
                        Array.Copy(data, 0, _receiveData, 0, NumberOfBytes);
                        // if (debug) StoreLogData.Instance.Store("Receive ModbusTCP-Data: " + BitConverter.ToString(receiveData), System.DateTime.Now);
                        ReceiveDataChanged(this);
                    }
                }
            }
            if (data[7] == 0x97 & data[8] == 0x01) {
                // if (debug) StoreLogData.Instance.Store("FunctionCodeNotSupportedException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.FunctionCodeNotSupportedException("Function code not supported by master");
            }
            if (data[7] == 0x97 & data[8] == 0x02) {
                // if (debug) StoreLogData.Instance.Store("StartingAddressInvalidException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.StartingAddressInvalidException("Starting address invalid or starting address + quantity invalid");
            }
            if (data[7] == 0x97 & data[8] == 0x03) {
                // if (debug) StoreLogData.Instance.Store("QuantityInvalidException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.QuantityInvalidException("quantity invalid");
            }
            if (data[7] == 0x97 & data[8] == 0x04) {
                // if (debug) StoreLogData.Instance.Store("ModbusException Throwed", System.DateTime.Now);
                throw new ModeBusHandler.Exceptions.ModbusException("error reading");
            }
            response = new int[quantityRead];
            for (int i = 0; i < quantityRead; i++) {
                byte lowByte;
                byte highByte;
                highByte = data[9 + i * 2];
                lowByte = data[9 + i * 2 + 1];

                data[9 + i * 2] = lowByte;
                data[9 + i * 2 + 1] = highByte;

                response[i] = BitConverter.ToInt16(data, (9 + i * 2));
            }
            return (response);
        }

        /// <summary>
        /// Close connection to Master Device.
        /// </summary>
        public void Disconnect()
        {
            // if (debug) StoreLogData.Instance.Store("Disconnect", System.DateTime.Now);
            if (_serialPort != null) {
                if (_serialPort.IsOpen & !this.receiveActive)
                    _serialPort.Close();
                if (ConnectedChanged != null)
                    ConnectedChanged(this);
                return;
            }
            if (stream != null)
                stream.Close();
            if (_tcpClient != null)
                _tcpClient.Close();
            _connected = false;
            if (ConnectedChanged != null)
                ConnectedChanged(this);

        }

        /// <summary>
        /// Destructor - Close connection to Master Device.
        /// </summary>
		~ModbusClient()
        {
            // if (debug) StoreLogData.Instance.Store("Destructor called - automatically disconnect", System.DateTime.Now);
            if (_serialPort != null) {
                if (_serialPort.IsOpen)
                    _serialPort.Close();
                return;
            }
            if (_tcpClient != null & !_udpFlag) {
                if (stream != null)
                    stream.Close();
                _tcpClient?.Close();
            }
        }

        /// <summary>
        /// Returns "TRUE" if Client is connected to Server and "FALSE" if not. In case of Modbus RTU returns if COM-Port is opened
        /// </summary>
		public bool Connected {
            get {
                if (_serialPort != null) {
                    return (_serialPort.IsOpen);
                }

                if (_udpFlag & _tcpClient != null)
                    return true;
                if (_tcpClient == null)
                    return false;
                else {
                    return _connected;

                }

            }
        }

        public bool Available(int timeout)
        {
            // Ping's the local machine.
            System.Net.NetworkInformation.Ping pingSender = new System.Net.NetworkInformation.Ping();
            IPAddress address = System.Net.IPAddress.Parse(_ipAddress == null ? string.Empty : _ipAddress);

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(data);

            // Wait 10 seconds for a reply.
            System.Net.NetworkInformation.PingReply reply = pingSender.Send(address, timeout, buffer);

            if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Gets or Sets the IP-Address of the Server.
        /// </summary>
		public string IPAddress {
            get {
                return _ipAddress == null ? string.Empty : _ipAddress;
            }
            set {
                _ipAddress = value;
            }
        }

        /// <summary>
        /// Gets or Sets the Port were the Modbus-TCP Server is reachable (Standard is 502).
        /// </summary>
		public int Port {
            get {
                return _port;
            }
            set {
                _port = value;
            }
        }

        /// <summary>
        /// Gets or Sets the UDP-Flag to activate Modbus UDP.
        /// </summary>
        public bool UDPFlag {
            get {
                return _udpFlag;
            }
            set {
                _udpFlag = value;
            }
        }

        /// <summary>
        /// Gets or Sets the Unit identifier in case of serial connection (Default = 0)
        /// </summary>
        public byte UnitIdentifier {
            get {
                return _unitIdentifier;
            }
            set {
                _unitIdentifier = value;
            }
        }


        /// <summary>
        /// Gets or Sets the Baudrate for serial connection (Default = 9600)
        /// </summary>
        public int Baudrate {
            get {
                return _baudRate;
            }
            set {
                _baudRate = value;
            }
        }

        /// <summary>
        /// Gets or Sets the of Parity in case of serial connection
        /// </summary>
        public Parity Parity {
            get {
                if (_serialPort != null)
                    return _parity;
                else
                    return Parity.Even;
            }
            set {
                if (_serialPort != null)
                    _parity = value;
            }
        }


        /// <summary>
        /// Gets or Sets the number of stopbits in case of serial connection
        /// </summary>
        public StopBits StopBits {
            get {
                if (_serialPort != null)
                    return _stopBits;
                else
                    return StopBits.One;
            }
            set {
                if (_serialPort != null)
                    _stopBits = value;
            }
        }

        /// <summary>
        /// Gets or Sets the connection Timeout in case of ModbusTCP connection
        /// </summary>
        public int ConnectionTimeout {
            get {
                return _connectTimeout;
            }
            set {
                _connectTimeout = value;
            }
        }

        /// <summary>
        /// Gets or Sets the serial Port
        /// </summary>
        public string SerialPort {
            get {

                return _serialPort?.PortName ?? string.Empty;
            }
            set {
                if (value == null) {
                    _serialPort = null;
                    return;
                }
                if (_serialPort != null)
                    _serialPort.Close();
                this._serialPort = new SerialPort();
                this._serialPort.PortName = value;
                _serialPort.BaudRate = _baudRate;
                _serialPort.Parity = _parity;
                _serialPort.StopBits = _stopBits;
                _serialPort.WriteTimeout = 10000;
                _serialPort.ReadTimeout = _connectTimeout;
                _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            }
        }
    }
}
