using Grumpy.ModeBusHandler;
using Grumpy.Common;
using Grumpy.HWControl.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;


namespace Grumpy.ClickPLC
{

    public class ClickHandler : IClickPLCHandler
    {
        internal const string TimerPrefix = "T";
        internal const string CounterPrefix = "CT";
        internal const string TimerCounterPrefix = "TD";
        internal const string CounterCounterPrefix = "CTD";

        private Dictionary<IOType, int> _sartWriteAddresses =
            new Dictionary<IOType, int>() {

                {IOType.Input, ClickAddressMap.XStartAddressHex},
                {IOType.Output, ClickAddressMap.YStartAddressHex},
                {IOType.ControlRelay, ClickAddressMap.CStartAddressHex},
                {IOType.RegisterInt16, ClickAddressMap.DSStartAddressHex},
                {IOType.Timer, ClickAddressMap.TStartAddressHex}
            };

        private Dictionary<IOType, int> _sartReadAddresses =
            new Dictionary<IOType, int>() {

                {IOType.Input, ClickAddressMap.XStartAddressHex},
                {IOType.Output, ClickAddressMap.YStartAddressHex},
                {IOType.ControlRelay, ClickAddressMap.CStartAddressHex},
                {IOType.SystemControlRelay, ClickAddressMap.SCStartAddressHex},
                {IOType.RegisterInt16, ClickAddressMap.DSStartAddressHex},
                {IOType.Timer, ClickAddressMap.TStartAddressHex}
            };

        private Dictionary<string, object> Controls;

        private StackBase<ClickHandlerException> _errorHistory;
        private ModbusClient? _mbClient;
        private ClickHandlerConfiguration? _configuration;

        internal ClickHandler() {

            _mbClient = new ModbusClient();
            _configuration = null;
            _errorHistory =
                new StackBase<ClickHandlerException>("Error History") { };
            Controls = new Dictionary<string, object>();
        }

        public static ClickHandler CreateHandler() => new ClickHandler();

        public static bool CreateHandler( ClickHandlerConfiguration configuration, 
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
            if( !handler.Init(configuration)) {
               handler = null;
            }
            return handler is not null;
        }


        public bool Init(ClickHandlerConfiguration configuration) {

            if (IsOpen) {
                _AddErrorRecord(nameof(Init),
                                       ErrorCode.ProhibitedWhenControllerIsConnected, "");
                return false;
            }

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
                     ErrorCode.ConfigDeserialisationError, ex.Message);
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


        /*  public RelayControl GetRelayControlRW(string relayName) {

              if (ClickAddressMap.GetModBusHexAddress(out int address,
                  control: relayName,
                  rtu: false) != ErrorCode.NoError) {

                  return null;
              }

              if (!Controls.ContainsKey(relayName.ToUpper())) {

                  Controls.Add(relayName.ToUpper(),
                      new RelayControl(relayName,
                      (v) => WriteDiscreteControl(relayName, v),
                      (out SwitchState r) => ReadDiscreteIO(relayName, out r)));
              }

              return Controls[relayName.ToUpper()] as RelayControl;
          }
        */
        /*
        public RelayControlRO GetRelayControlRO(string relayName) {

            if (ClickAddressMap.GetModAddress(out int address,
                control: relayName,
                rtu: false) != ErrorCode.NoError) {

                return null;
            }

            if (!Controls.ContainsKey(relayName.ToUpper())) {

                Controls.Add(relayName.ToUpper(),
                    new RelayControlRO(relayName,
                    (out SwitchState r) => ReadDiscreteIO(relayName, out r)));
            }

            return Controls[relayName.ToUpper()] as RelayControlRO;
        }
        */
        /*
        public RegisterInt16Control GetRegisterInt16Control(string registerName) {

            if (ClickAddressMap.GetModAddress(out int address,
                        control: registerName, rtu: false) != ErrorCode.NoError) {
                return null;
            }

            if (!Controls.ContainsKey(registerName.ToUpper())) {

                Controls.Add(registerName.ToUpper(),
                    new RegisterInt16Control(registerName,
                         (v) => WriteRegister(registerName, v),
                         (out ushort r) => ReadRegister(registerName, out r)));
            }

            return Controls[registerName.ToUpper()] as RegisterInt16Control;
        }
        */
        /*
        public RegisterInt16ControlRO GetRegisterInt16ControlRO(
                        string registerName) {

            if (ClickAddressMap.GetModAddress(out int address,
                    control: registerName, rtu: false) != ErrorCode.NoError) {

                return null;
            }

            if (!Controls.ContainsKey(registerName.ToUpper())) {

                Controls.Add(registerName.ToUpper(),
                             new RegisterInt16ControlRO(registerName,
                                                                                                                           (out ushort r) => ReadRegister(registerName, out r)));
            }

            return Controls[registerName.ToUpper()] as RegisterInt16ControlRO;
        }
        */
        /*
        public TimerCounter GetTimerCtrl(string timerName,
                       string setPointName = null,
                       string resetName = null,
                       bool canWriteReset = false) =>
              _GetTimerCounter(timerName, setPointName,
                  resetName, canWriteReset, timer: true);

        public TimerCounter GetCounterCtrl(string counterName,
                       string setPointName = null,
                       string resetName = null,
                        bool canWriteReset = false) =>
              _GetTimerCounter(counterName, setPointName,
                  resetName, canWriteReset, timer: false);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected TimerCounter _GetTimerCounter(string name,
                       string setPointName = null,
                       string resetName = null,
                       bool canWriteReset = false,
                       bool timer = true) {

            if (string.IsNullOrEmpty(name)
               || (timer && !name.StartsWith(TimerPrefix))
               || (timer && name.StartsWith(TimerCounterPrefix))
               || (!timer && !name.StartsWith(CounterPrefix))
               || (!timer && name.StartsWith(CounterCounterPrefix))) {

                _AddErrorRecord(nameof(_GetTimerCounter),
                       ErrorCode.InvalidControlName, $"Invalid " +
                       $"{(timer ? "timer" : "couner")} " +
                       $"name \"{name}\" provided.");
                
                return null;
            }

            RelayControlRO timerCtrl = GetRelayControlRO(name);

            
            string counterRegisterName = timer
                ? name.ToUpper().Replace(TimerPrefix, TimerCounterPrefix)
                : name.ToUpper().Replace(CounterPrefix, CounterCounterPrefix);

            RegisterInt16ControlRO counterCtrl =
                GetRegisterInt16ControlRO(counterRegisterName);

            RegisterInt16Control setPointCtrl =
                string.IsNullOrEmpty(setPointName) ? 
                        null : GetRegisterInt16Control(setPointName);

            RelayControl resetCtrl =
                string.IsNullOrEmpty(resetName) ? 
                        null : GetRelayControlRW(resetName);



            return new TimerCounter(timerCtrl, counterCtrl,
                setPointCtrl, resetCtrl, canWriteReset);
        }
        */

        public bool ReadRegister(string name, out ushort value) {

            value = 0xFFFF;

            if (!(_mbClient?.IsConnected ?? false)) {

                _AddErrorRecord(nameof(ReadRegister),
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

                    _AddErrorRecord(nameof(ReadRegister),
                                    ErrorCode.NotWritableControl,
                                    $"\"{address}\" control is not " +
                                    $"writable. {ex.Message}");
                    return false;
                }
            }
            else {
                _AddErrorRecord(nameof(ReadRegister),
                                ErrorCode.InvalidControlName,
                                $"Invalid control name \"{name}\".");
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

        public bool WriteRegister(string name, ushort value) {

            if (_DecodeControlName(name, out IOType ioType, out int address, write: true)) {

                return _mbClient?.WriteSingle16bitRegister(address, value) ?? false;
            }

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

                _AddErrorRecord(nameof(_DecodeControlName), ErrorCode.InvalidControlNamePreffix);
                return false;
            }

            IOType tp = ChannelConstants.IoTypes[preffix];
            var selector = write ? _sartWriteAddresses : _sartReadAddresses;

            if (selector.ContainsKey(tp)) {
                try {

                    address = selector[tp];
                    int idx = name.IndexOf(preffix);
                    address += Int32.Parse(name.ToUpper().Substring(idx + preffix.Length)) - 1;
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

        private bool _AddErrorRecord(string methodName,
                                    ErrorCode code, string? details = null) {
            try {
                _errorHistory.Push(
                        new ClickHandlerException(methodName,
                        ErrorCode.ConfigDeserialisationError,
                        details), force: true);
                return true;
            }
            catch (Exception ex) {

#if DEBUG
                string msg = $"Failed to add error record to the error " +
                    $"history stack. Exception: {ex.Message}";
                Console.WriteLine(msg);
#endif

                return false;
            }
        }

        #endregion Private Methods
    }
}
