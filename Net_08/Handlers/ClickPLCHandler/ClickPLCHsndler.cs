using Grumpy.Common;

using Grumpy.HWControl.Configuration;
using Grumpy.HWControl.Interfaces;
using Grumpy.HWControl.IO;
using Grumpy.ModeBusHandler;

using LV.ClickPLCHandler;
using Newtonsoft.Json;
using System.Numerics;
using System.Runtime.CompilerServices;


namespace Grumpy.ClickPLC
{

    public class ClickHandler : IPLCHandler {
        internal const string TimerPrefix = "T";
        internal const string CounterPrefix = "CT";
        internal const string TimerDataRegisterPrefix = "TD";
        internal const string CounterDataRegisterPrefix = "CTD";

        private Dictionary<IOType, int> _startWriteAddresses =
            new Dictionary<IOType, int>() {

                {IOType.Input, ClickAddressMap.XStartAddressHex},
                {IOType.Output, ClickAddressMap.YStartAddressHex},
                {IOType.ControlRelay, ClickAddressMap.CStartAddressHex},
                {IOType.RegisterInt16, ClickAddressMap.DSStartAddressHex},
                {IOType.Timer, ClickAddressMap.TStartAddressHex},
                {IOType.RegisterFloat32, ClickAddressMap.DFStartAddressHex}
            };

        private Dictionary<IOType, int> _startReadAddresses =
            new Dictionary<IOType, int>() {

                {IOType.Input, ClickAddressMap.XStartAddressHex},
                {IOType.Output, ClickAddressMap.YStartAddressHex},
                {IOType.ControlRelay, ClickAddressMap.CStartAddressHex},
                {IOType.SystemControlRelay, ClickAddressMap.SCStartAddressHex},
                {IOType.RegisterInt16, ClickAddressMap.DSStartAddressHex},
                {IOType.Timer, ClickAddressMap.TStartAddressHex},
                {IOType.RegisterFloat32, ClickAddressMap.DFStartAddressHex}
            };

        private Dictionary<string, object> Controls;

        private StackBase<ILogRecord> _history;
        private ModbusClient? _mbClient;
        private ClickHandlerConfiguration? _configuration;

        internal ClickHandler() {

            _mbClient = new ModbusClient();
            _configuration = null;
            _history =
                new StackBase<ILogRecord>("Log History") { };
            Controls = new Dictionary<string, object>();
        }

        public static ClickHandler CreateHandler() => new ClickHandler();

        public static bool CreateHandler(ClickHandlerConfiguration configuration,
            out ClickHandler? handler) {

            handler = new ClickHandler();
            if (!handler.Init(configuration)) {
                handler = null;
            }
            return handler is not null;
        }

        public static bool CreateHandler(string configuration,
            out ClickHandler? handler) {

            handler = new ClickHandler();
            if (!handler.Init(configuration)) {
                handler = null;
            }
            return handler is not null;
        }


        public bool Init(object cnfg) {


            if (IsOpen) {
                _AddErrorRecord(nameof(Init),
                                       ErrorCode.ProhibitedWhenControllerIsConnected, "");
                return false;
            }

            var configuration = cnfg as ClickHandlerConfiguration;

            if (configuration == null) {
                _AddErrorRecord(nameof(Init),
                    ErrorCode.ConfigurationIsNotProvided,
                    "Provided configuration object is \"null\"");
                return false;
            }

            _configuration = configuration.Clone() as ClickHandlerConfiguration;

            if (_configuration == null) {
                _AddErrorRecord(nameof(Init),
                                ErrorCode.ConfigurationNotSet,
                                "Failed to clone provided configuration object.");
            }

            return _configuration != null;
        }

        public bool Init(string configJsonString) {

            if (IsOpen) {
                _AddErrorRecord(nameof(Init),
                    ErrorCode.ProhibitedWhenControllerIsConnected, "");
                return false;
            }

            if (string.IsNullOrEmpty(configJsonString)) {
                _AddErrorRecord(nameof(Init),
                    ErrorCode.ConfigurationIsNotProvided,
                    "Provided configuration string is \"null\" or empty.");
                return false;
            }

            try {
                _configuration =
                    JsonConvert.DeserializeObject<ClickHandlerConfiguration>(configJsonString);
                return true;
            }
            catch (Exception ex) {
                _AddErrorRecord(nameof(Init),
                     ErrorCode.ConfigDeserializationError, ex.Message);
                return false;
            }
        }


        private bool _OpenSerial() {
            if (_configuration?.Interface?.SerialPort is null) {
                _AddErrorRecord(nameof(Open),
                                       ErrorCode.OpenFailed,
                                                          "Serial port configuration is not provided.");
                return false;
            }

            _mbClient = new ModbusClient(_configuration.Interface.SerialPort);
            return _mbClient.Connect();
        }

        private bool _OpenTcpIp() {

            if (_configuration?.Interface?.Network is null) {
                _AddErrorRecord(nameof(Open),
                                       ErrorCode.OpenFailed,
                                                          "Network configuration is not provided.");
                return false;
            }
            else if (_configuration.Interface.Network.IpAddress is null
                               || _configuration.Interface.Network.Port < 0) {
                _AddErrorRecord(nameof(Open),
                                       ErrorCode.OpenFailed,
                                                          $"IP configuration is not valid.");
                return false;
            }

            _mbClient = new ModbusClient(
                               _configuration.Interface.Network.IpAddress,
                                              _configuration.Interface.Network.Port);

            return _mbClient.Connect();
        }

        private bool _DisconnectIfConnected([CallerMemberName] string callerMethodName = "") {

            if (_mbClient != null && _mbClient.IsConnected) {

                try {
                    _mbClient.Disconnect();
                    return true;
                }
                catch (Exception ex) {
                    _AddErrorRecord(callerMethodName,
                                                   ErrorCode.CloseFailed, ex.Message);
                    return false;
                }
            }

            return true;
        }

        public bool Open() {

            if (_configuration == null) {

                _AddErrorRecord(nameof(Open),
                    ErrorCode.ConfigurationNotSet);
                return false;
            }

            if (_DisconnectIfConnected()) {

                switch (_configuration?.Interface?.Selector ?? InterfaceSelector.Auto) {

                    case InterfaceSelector.Serial:
                        return _OpenSerial();

                    case InterfaceSelector.Network:
                        return _OpenTcpIp();

                    default:
                        return _OpenTcpIp() ? true : _OpenSerial();

                }
            }
            return false;
        }

        public bool Close() {
            try {
                _mbClient?.Disconnect();
                return true;
            }
            catch (Exception ex) {

                _AddErrorRecord(nameof(Close), ErrorCode.CloseFailed,
                    ClickPlcHandlerErrors.GetErrorDescription(
                                                ErrorCode.CloseFailed)
                    + $" Exception: {ex.Message}");
                return false;
            }
        }

        public bool IsOpen => _mbClient?.IsConnected ?? false;

        public ILogRecord? LastRecord {
            get {
                if (_history.Peek(out ILogRecord ex)) {
                    return ex;
                }
                return null;
            }
        }

        public bool WriteDiscreteControl(string name, SwitchCtrl sw) {

            if (!(_mbClient?.IsConnected ?? false)) {

                _AddErrorRecord(nameof(WriteDiscreteControl),
                    ErrorCode.NotConnected,
                    $"Can't write when not connected.");
                return false;
            }

            if (ClickAddressMap.GetModBusHexAddress(
                   ioFunction: IoFunction.SingleControlWrite,
                   control: name, out int address,
                   out int functionCode) == ErrorCode.NoError) {

                try {

                    _mbClient.WriteSingleCoil(address,
                        sw == SwitchCtrl.On, functionCode);
                    return true;
                }
                catch (Exception ex) {

                    _AddErrorRecord(nameof(WriteDiscreteControl),
                        ErrorCode.NotWritableControl,
                        $"\"{address}\" control is not writable. {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        public bool WriteDiscreteControls(string startName, SwitchCtrl[] controls) {

            if (!(_mbClient?.IsConnected ?? false)) {

                _AddErrorRecord(nameof(WriteDiscreteControls),
                                       ErrorCode.NotConnected,
                                                          $"Can't write when not connected.");
                return false;
            }

            if (_DecodeControlName(startName, out IOType ioType,
                               out int address, write: true)) {

                try {

                    _mbClient.WriteMultipleCoils(address,
                                               controls.Select((c) => c == SwitchCtrl.On).ToArray());
                    return true;
                }
                catch (Exception ex) {

                    _AddErrorRecord(nameof(WriteDiscreteControls),
                                               ErrorCode.GroupIoWriteFailed,
                                                                      ex.Message);
                    return false;
                }
            }
            return false;
        }


        public bool ReadDiscreteIO(string name, out SwitchState state) {
            bool r = ReadDiscreteIO(name, out SwitchSt st);
            state = new SwitchState(st);
            return r;
        }

        internal bool ReadDiscreteIO(string name, out SwitchSt status) {


            if (!(_mbClient?.IsConnected ?? false)) {

                _AddErrorRecord(nameof(ReadDiscreteIO),
                    ErrorCode.NotConnected,
                    $"Can't read  when not connected.");
                status = SwitchSt.Unknown;
                return false;
            }

            if (ClickAddressMap.GetModBusHexAddress(
                    ioFunction: IoFunction.SingleControlRead,
                    control: name, out int address,
                    out int functionCode) == ErrorCode.NoError) {

                try {

                    _mbClient.ReadCoils(address, 1, out bool[]? data, functionCode);
                    if (data is not null) {
                        status = data[0] ? SwitchSt.On : SwitchSt.Off;

                        return true;
                    }
                    status = SwitchSt.Unknown;
                    return false;
                }
                catch (Exception ex) {

                    _AddErrorRecord(nameof(ReadDiscreteIO),
                        ErrorCode.GroupIoWriteFailed,
                        ex.Message);
                    status = SwitchSt.Unknown;
                    return false;
                }
            }
            else {

                _AddErrorRecord(nameof(ReadDiscreteIO),
                            ErrorCode.NotConnected,
                            $"Invalid address \"{name}\".");
                status = SwitchSt.Unknown;
                return false;
            }
        }



        public bool ReadDiscreteIOs(string name, int numberOfIosToRead, out SwitchState[] status) {

            if (!(_mbClient?.IsConnected ?? false)) {
                _AddErrorRecord(nameof(ReadDiscreteIO),
                    ErrorCode.NotConnected,
                    $"Can't read  when not connected.");
                status = new SwitchState[] { };
                return false;
            }

            if (ClickAddressMap.GetModBusHexAddress(ioFunction: IoFunction.MultipleControlRead,
                control: name, out int address, out int functionCode) == ErrorCode.NoError) {

                try {

                    if (_mbClient.ReadCoils(address,

                        Math.Max(1, numberOfIosToRead), out bool[]? data, functionCode)) {

                        if (data is not null) {

                            status =
                                data.Select((st) => new SwitchState(st ? SwitchSt.On : SwitchSt.Off)).ToArray();
                            return true;
                        }
                    }
                }
                catch (Exception ex) {

                    _AddErrorRecord(nameof(ReadDiscreteIO),
                        ErrorCode.GroupIoWriteFailed,
                        ex.Message);
                }
            }
            else {

                _AddErrorRecord(nameof(ReadDiscreteIOs),
                            ErrorCode.NotConnected,
                            $"Invalid start address \"{name}\".");
            }

            status = Enumerable.Repeat(new SwitchState(SwitchSt.Unknown), numberOfIosToRead).ToArray();
            return false;
        }


        public bool ReadInt16Register(string name, out int value) {

            value = -1;

            if (!(_mbClient?.IsConnected ?? false)) {

                _AddErrorRecord(nameof(ReadInt16Register),
                                       ErrorCode.NotConnected,
                                                          $"Can't write when not connected.");
                return false;
            }

            if (ClickAddressMap.GetModBusHexAddress(ioFunction: IoFunction.SingleControlRead,
                               control: name, out int address, out int functionCode) == ErrorCode.NoError) {

                try {

                    _mbClient.ReadInputRegisters(address, 1, out int[]? response, functionCode);
                    value = (short)(response?[0] ?? -1);
                    return true;
                }
                catch (Exception ex) {

                    _AddErrorRecord(nameof(ReadInt16Register),
                                                           ErrorCode.NotWritableControl,
                                                                                              $"\"{address}\" control is not " +
                                                                                                                                 $"writable. {ex.Message}");
                    return false;
                }
            }
            else {
                _AddErrorRecord(nameof(ReadInt16Register),
                                                   ErrorCode.InvalidControlName,
                                                                                  $"Invalid control name \"{name}\".");
                return false;
            }
        }


        public bool ReadUInt16Register(string name, out ushort value) {

            if (ReadInt16Register(name, out int valueInt16)) {

                value = (ushort)Math.Max(0, valueInt16);
                return true;
            }


                value = 0xFFFF;

            if (!(_mbClient?.IsConnected ?? false)) {

                _AddErrorRecord(nameof(ReadUInt16Register),
                    ErrorCode.NotConnected,
                    $"Can't write when not connected.");
                return false;
            }

            if (ClickAddressMap.GetModBusHexAddress(ioFunction: IoFunction.SingleControlRead,
                control: name, out int address, out int functionCode) == ErrorCode.NoError) {

                try {

                    _mbClient.ReadInputRegisters(address, 1, out int[]? response, functionCode);
                    value = (ushort)(response?[0] ?? 0xFFFF);
                    return true;
                }
                catch (Exception ex) {

                    _AddErrorRecord(nameof(ReadUInt16Register),
                                    ErrorCode.NotWritableControl,
                                    $"\"{address}\" control is not " +
                                    $"writable. {ex.Message}");
                    return false;
                }
            }
            else {
                _AddErrorRecord(nameof(ReadUInt16Register),
                                ErrorCode.InvalidControlName,
                                $"Invalid control name \"{name}\".");
                return false;
            }
        }

        public bool Write16BitRegister(string name, ushort value) {

            if (_DecodeControlName(name, out IOType ioType, out int address, write: true)) {

                return _mbClient?.WriteSingle16bitRegister(address, value) ?? false;
            }

            return false;
        }


        public bool WriteFloat32BitRegister(string name, float value) {

            if (_DecodeControlName(name, out IOType ioType, out int address, write: true)) {
                try {
                    var res = Utilities.ConvertFloatToRegisters(value);
                    _mbClient?.WriteMultipleRegisters(address, res);
                    return true;    
                }
                catch (Exception ex) {
                    _AddErrorRecord(nameof(WriteFloat32BitRegister),
                                               ErrorCode.InvalidControlName, ex.Message);
                    return false;
                }
            }

             return false;   
        }

        public bool ReadFloat32BitRegister(string name, out float value) {

            if (_DecodeControlName(name, out IOType ioType, out int address, write: false)) {
                try {

                    int[]? data = null;
                    Boolean res = _mbClient?.ReadInputRegisters(address, 2, out data) ?? false;

                    string? error = null;
                    if (res && Utilities.ConvertRegistersToFloat(data,  RegisterOrder.LowHigh , out value, out error)) {

                        return true;
                    }
                    else {
                        _AddErrorRecord(nameof(ReadFloat32BitRegister), ErrorCode.FailedTConvertRegistersToFloat,
                                    $"{nameof(ReadFloat32BitRegister)}  {error ?? string.Empty}");
                    }                                                
                }
                catch (Exception ex) {
                    _AddErrorRecord(nameof(ReadFloat32BitRegister), ErrorCode.InvalidControlName, ex.Message);
                }
            }

            value = float.NaN;
            return false;
        }   






        #region Private Methods

        private bool _DecodeControlName(string name, out IOType ioType,
        out int address, bool write) {
            ioType = IOType.Unknown;
            address = -1;

            var preffix =
                ChannelConstants.ValidControlNamePreffixes
                .FirstOrDefault((x) => name.ToUpper().StartsWith(x.ToUpper()));

            if (string.IsNullOrEmpty(preffix)) {

                _AddErrorRecord(nameof(_DecodeControlName), ErrorCode.InvalidControlNamePrefix);
                return false;
            }

            IOType tp = ChannelConstants.IoTypes[preffix];
            var selector = write ? _startWriteAddresses : _startReadAddresses;

            if (selector.ContainsKey(tp)) {
                try {

                    address = selector[tp];
                    int idx = Int32.Parse(name.Substring(name.IndexOf(preffix) + preffix.Length));
                    address +=_CalculateAddressOffst(tp, idx);
                    ioType = tp;
                    return true;
                }
                catch (Exception ex) {

                    _AddErrorRecord(nameof(_DecodeControlName), ErrorCode.InvalidControlName, ex.Message);
                    return false;
                }
            }

            _AddErrorRecord(nameof(_DecodeControlName), ErrorCode.IoNotSupported);
            return false;
        }

        private static int _CalculateAddressOffst(IOType type, int idx) {

            switch (type) {
                case IOType.RegisterFloat32:
                case IOType.RegisterInt32:
                    return Math.Max(0, idx - 1) * 2;

                default:
                    return Math.Max(0, idx -1);
            }
        }

        private bool _AddErrorRecord(string methodName,
                                    ErrorCode code, string? details = null) {
            try {
                _history.Push(
                        new LogRecord(LogLevel.Error, methodName, details!,  (int) code, DateTime.Now));
                return true;
            }
#if !DEBUG
            catch {
                return false;
            }
#else
            catch (Exception ex) {
                string msg = $"Failed to add error record to the error " +
                    $"history stack. Exception: {ex.Message}";
                Console.WriteLine(msg);
                      return false;
            }
#endif
        }

#endregion Private Methods
    }
}
