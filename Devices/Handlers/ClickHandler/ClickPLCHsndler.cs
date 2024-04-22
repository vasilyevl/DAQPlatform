using EasyModbus;
using LV.Common;
using LV.HWControl.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;


namespace LV.ClickPLCHandler
{
    public interface IClickHandler { }

    public class ClickPLCHandler
    {
        internal const string TimerPrefix = "T";
        internal const string CounterPrefix = "CT";
        internal const string TimerCounterPrefix = "TD";
        internal const string CounterCounterPrefix = "CTD";

        private Dictionary<IOType, int> _sartWriteAddresses = 
            new Dictionary<IOType, int>()
            {
                {IOType.Input, ClickAddressMap.XStartAddressHex},
                {IOType.Output, ClickAddressMap.YStartAddressHex},
                {IOType.ControlRelay, ClickAddressMap.CStartAddressHex},
                {IOType.RegisterInt16, ClickAddressMap.DSStartAddressHex},
                {IOType.Timer, ClickAddressMap.TStartAddressHex}
            };

        private Dictionary<IOType, int> _sartReadAddresses = 
            new Dictionary<IOType, int>()
        {
            {IOType.Input, ClickAddressMap.XStartAddressHex},
            {IOType.Output, ClickAddressMap.YStartAddressHex},
            {IOType.ControlRelay, ClickAddressMap.CStartAddressHex},
            {IOType.SystemControlRelay, ClickAddressMap.SCStartAddressHex},
            {IOType.RegisterInt16, ClickAddressMap.DSStartAddressHex},
            {IOType.Timer, ClickAddressMap.TStartAddressHex}
        };

        private Dictionary<string, object> Controls;

        private StackBase<ClickHandlerException> _errorHistory;
        private ModbusClient _mbClient;
        private ClickHandlerConfiguration _configuration;

        internal ClickPLCHandler()
        {
            _mbClient = new ModbusClient();
            _configuration = null;
            _errorHistory = new StackBase<ClickHandlerException>("Error History") { };
            Controls = new Dictionary<string, object>();
        }

        public static ClickPLCHandler CreateHandler() => new ClickPLCHandler();

        public static ClickPLCHandler CreateHandler(ClickHandlerConfiguration configuration)
        {
            var h = new ClickPLCHandler();
            h.Init(configuration);
            return h;
        }

        public bool Init(ClickHandlerConfiguration cnfg)
        {
            if (cnfg == null)
            {
                _AddErrorRecord(nameof(Init),
                    ErrorCode.ConfigurationIsNotProvided,
                    "Provided configuration object is \"null\"");
                return false;
            }

            var jobject = JObject.FromObject(_configuration);
            _configuration = jobject.ToObject<ClickHandlerConfiguration>();

            _configuration.CopyFrom(cnfg);
            return true;
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

        public bool Open() {
            if (_configuration == null) {

                _AddErrorRecord(nameof(Open),
                    ErrorCode.ConfigurationNotSet);
                return false;
            }

            if (_mbClient != null) {
                if (_mbClient.IsConnected) {
                    try {
                        _mbClient.Disconnect();
                        _mbClient = null;
                    }
                    catch (Exception ex) {

                        _AddErrorRecord(nameof(Open), ErrorCode.CloseFailed, ex.Message);
                    }
                }
            }

            switch (_configuration.Interface.Selector) {

                case InterfaceSelector.Serial:

                    _mbClient = new ModbusClient(_configuration.Interface.SerialPort);

                    if (_mbClient.Connect()) {
                        return true;
                    }
                    break;

                case InterfaceSelector.Network:
                    _mbClient = new ModbusClient(
                        _configuration.Interface.Network.IpAddress,
                        _configuration.Interface.Network.Port);

                    if (_mbClient.Connect()) {
                        return true;
                    }
                    break;

                default:

                    _mbClient = new ModbusClient(
                        _configuration.Interface.Network.IpAddress,
                        _configuration.Interface.Network.Port);

                    if (_mbClient.Connect()) {
                        return true;
                    }

                    _mbClient = new ModbusClient(_configuration.Interface.SerialPort);
                    if (_mbClient.Connect()) {
                        return true;
                    }
                    break;
            }

            _AddErrorRecord(nameof(Open),
                            ErrorCode.OpenFailed,
                            (string)_mbClient.LastError.Clone());
            return false;
        }

        private bool _AddErrorRecord(string methodName,
            ErrorCode code, string details = null) {
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

        public bool Close() {
            try {
                _mbClient.Disconnect();
                return true;
            }
            catch (Exception ex) {

                _AddErrorRecord(nameof(Close), ErrorCode.CloseFailed,
                    ClickPlcHandlerErrors.GetErrorDescription(ErrorCode.CloseFailed)
                    + $" Exception: {ex.Message}");
                return false;
            }
        }

        public bool IsOpen => _mbClient.IsConnected;


        public RelayControl GetRelayControlRW(string relayName) {

            if (ClickAddressMap.GetModAddress(out int address,
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
                       $"{(timer? "timer" : "couner")} " +
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
                string.IsNullOrEmpty(setPointName) ? null : GetRegisterInt16Control(setPointName);

            RelayControl resetCtrl =  
                string.IsNullOrEmpty(resetName) ? null : GetRelayControlRW(resetName);

            return new TimerCounter(timerCtrl, counterCtrl, 
                setPointCtrl, resetCtrl, canWriteReset);
        }


        public bool WriteDiscreteControl(string name, SwitchCtrl sw) {
            
            if (!(_mbClient?.IsConnected ?? false)) {

                _AddErrorRecord(nameof(WriteDiscreteControl),
                    ErrorCode.NotConnected,
                    $"Can't write when not connected.");
                return false;
            }

            if (ClickAddressMap.GetModAddress(out int address, control: name, rtu: false) == ErrorCode.NoError) {

                try {

                    _mbClient.WriteSingleCoil(address, sw == SwitchCtrl.On);
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

        public bool WriteDiscreteIOs(string startName,
                                      SwitchCtrl[] ctrls) {
            if (!(_mbClient?.IsConnected ?? false)) {

                _AddErrorRecord(nameof(WriteDiscreteIOs),
                    ErrorCode.NotConnected,
                    $"Can't write when not connected.");
                return false;
            }

            if (_DecodeControlName(startName, out IOType ioType,
                    out int address, true)) {

                if ((ctrls?.Count() ?? 0) < 1) {

                    _AddErrorRecord(nameof(WriteDiscreteIOs),
                        ErrorCode.NoDataProvided,
                        $"No data provided fo writing");
                    return false;
                }

                try {

                    var cts = ctrls.Select((ctrl) => ctrl == SwitchCtrl.On).ToArray();
                    _mbClient.WriteMultipleCoils(address,
                        ctrls.Select((ctrl) => ctrl == SwitchCtrl.On).ToArray());
                    return true;
                }
                catch (Exception ex) {

                    _AddErrorRecord(nameof(WriteDiscreteIOs),
                        ErrorCode.GroupIoWriteFailed,
                        ex.Message);
                    return false;
                }
            }
            return false;
        }

        public bool ReadDiscreteIO(string name, out SwitchState state)
        {
            bool r = ReadDiscreteIO(name, out SwitchSt st);
            state = new SwitchState(st);
            return r;
        }

        public bool ReadDiscreteIO(string name, out SwitchSt status) {
            if (ReadDiscreteIOs(name, 1, out SwitchSt[] st)) {

                status = st[0];
                return true;
            }

            if (!(_mbClient?.IsConnected ?? false)) {

                _AddErrorRecord(nameof(ReadDiscreteIO),
                    ErrorCode.NotConnected,
                    $"Can't read  when not connected.");
                status = SwitchSt.Unknown;
                return false;
            }

            if (_DecodeControlName(name, out IOType ioType,
                            out int address, true)) {

                try {

                    _mbClient.ReadCoils(address, 1, out bool[] data);
                    status = data[0] ? SwitchSt.On : SwitchSt.Off;
                    return true;
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

        public bool ReadDiscreteIOs(string name, int numberOfIosToRead, 
            out SwitchSt[] status) {
            if (!(_mbClient?.IsConnected ?? false)) {

                _AddErrorRecord(nameof(ReadDiscreteIOs),
                    ErrorCode.NotConnected,
                    $"Can't read  when not connected.");
                status = null;
                return false;
            }

            if (_DecodeControlName(name, out IOType ioType,
                                   out int address, true)) {

                try {

                    _mbClient.ReadCoils(address,
                        Math.Max(1, numberOfIosToRead), out bool[] data);
                    status =
                        data.Select((st) => st ? SwitchSt.On : SwitchSt.Off).ToArray();
                    return true;
                }
                catch (Exception ex) {

                    _AddErrorRecord(nameof(ReadDiscreteIO),
                        ErrorCode.GroupIoWriteFailed,
                        ex.Message);
                    status = Enumerable.Repeat(SwitchSt.Unknown, numberOfIosToRead).ToArray();
                    return false;
                }
            }
            else {

                _AddErrorRecord(nameof(ReadDiscreteIOs),
                            ErrorCode.NotConnected,
                            $"Invalid start address \"{name}\".");
                status = Enumerable.Repeat(SwitchSt.Unknown, numberOfIosToRead).ToArray();
                return false;
            }
        }

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

        public bool WriteRegister(string name, ushort value) {
            
            if (_DecodeControlName(name, out IOType ioType, out int address, write: true)) {

                return _mbClient.WriteSingle16bitRegister(address, value);
            }

            return false;
        }

        public bool ReadRegister(string name, out ushort value)
        {
            value = 0xFFFF;
            if (_DecodeControlName(name, out IOType ioType, out int address, write: false))
            {

                if (_mbClient.ReadInputRegisters(address, 1, out int[] response))
                {

                    value = (ushort)response[0];
                    return true;
                }
            }

            return false;
        }
    }
}
