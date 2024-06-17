/*
Copyright (c) 2024 LV-PissedEngineer Permission is hereby granted, 
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

using ModeBusHandler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PissedEngineer.HWControl;
using PissedEngineer.HWControl.Handlers;
using PissedEngineer.Primitives.Utilities;
using PissedEngineer.Primitives.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PissedEngineer.ClickPLCHandler
{

    public static class ClickHandlerFactory {         
      
        public static IClickPLCHandler CreateHandler() => 
                        new ClickPLCHandler();

        public static IClickPLCHandler CreateHandler(
                        ClickHandlerConfiguration configuration) {
            try {
                var h = new ClickPLCHandler();
                h.Init(configuration);
                return h;
            } catch (Exception ex) {
                   return null;
            }
        }

        public static IClickHandlerConfiguration CreateClickHandlerConfiguration() =>
            new ClickHandlerConfiguration();

        public static IClickHandlerConfiguration CreateClickHandlerConfiguration(
            IClickHandlerConfiguration cnfg) => new ClickHandlerConfiguration(cnfg);

        public static IClickHandlerConfiguration CreateClickHandlerConfiguration(
           string ipAddress, int port, string name = "ClickPLC") => 
            
            new ClickHandlerConfiguration() {
                Interface = new InterfaceConfiguration() {
                    Selector = InterfaceSelector.Network,
                    SerialPort = null,
                    Network =  
                    HwControlObjectFactory.CreateEthernetConnectionConfiguration( 
                        name, ipAddress, port)
                }
            };
    }

    public class ClickPLCHandler : IClickPLCHandler
    {
        internal const string TimerPrefix = "T";
        internal const string CounterPrefix = "CT";
        internal const string TimerCounterPrefix = "TD";
        internal const string CounterCounterPrefix = "CTD";

        private Dictionary<IOType, int> _startWriteAddresses =
            new Dictionary<IOType, int>() {

                {IOType.Input, ClickAddressMap.XStartAddressHex},
                {IOType.Output, ClickAddressMap.YStartAddressHex},
                {IOType.ControlRelay, ClickAddressMap.CStartAddressHex},
                {IOType.RegisterInt16, ClickAddressMap.DSStartAddressHex},
                {IOType.Timer, ClickAddressMap.TStartAddressHex}
            };

        private Dictionary<IOType, int> _startReadAddresses =
            new Dictionary<IOType, int>() {

                {IOType.Input, ClickAddressMap.XStartAddressHex},
                {IOType.Output, ClickAddressMap.YStartAddressHex},
                {IOType.ControlRelay, ClickAddressMap.CStartAddressHex},
                {IOType.SystemControlRelay, ClickAddressMap.SCStartAddressHex},
                {IOType.RegisterInt16, ClickAddressMap.DSStartAddressHex},
                {IOType.Timer, ClickAddressMap.TStartAddressHex}
            };

        private Dictionary<string, object> Controls;

        private LogHistory _errorHistory;
        private ModbusClient _mbClient;
        private ClickHandlerConfiguration _configuration;

        internal ClickPLCHandler() {

            _mbClient = new ModbusClient();
            _configuration = null;
            _errorHistory =
                new LogHistory() { };
            Controls = new Dictionary<string, object>();
        }

        public bool Init(IClickHandlerConfiguration cnfg) {
            if (cnfg == null) {
                _AddErrorRecord(nameof(Init),
                    ErrorCode.ConfigurationIsNotProvided,
                    "Provided configuration object is \"null\"");
                return false;
            }
            _configuration = new  ClickHandlerConfiguration(cnfg);
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

                        _AddErrorRecord(nameof(Open),
                            ErrorCode.CloseFailed, ex.Message);
                    }
                }
            }

            switch (_configuration.Interface.Selector) {

                case InterfaceSelector.Serial:

                    _mbClient =
                        new  ModbusClient(_configuration.Interface.SerialPort.Name);

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

                    _mbClient =
                        new ModbusClient(_configuration.Interface.SerialPort.Name);

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

        public bool Close() {
            try {
                _mbClient.Disconnect();
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

        public bool IsOpen => _mbClient.IsConnected;

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

        public bool WriteDiscreteControls(string startName, SwitchCtrl[] ctrls) {

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
                                               ctrls.Select((c) => c == SwitchCtrl.On).ToArray());
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

        public bool ReadDiscreteIO(string name, out SwitchSt status) {


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

                    _mbClient.ReadCoils(address, 1, out bool[] data, functionCode);
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

                    _mbClient.ReadInputRegisters(address, 1, out int[] response, functionCode);
                    value = (ushort)response[0];
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
                status = null;
                return false;
            }

            if (ClickAddressMap.GetModBusHexAddress(ioFunction: IoFunction.MultipleControlRead,
                control: name, out int address, out int functionCode) == ErrorCode.NoError) {

                try {

                    if (_mbClient.ReadCoils(address,
                        Math.Max(1, numberOfIosToRead), out bool[] data, functionCode)) {
                        status =
                            data.Select((st) => new SwitchState(st ? SwitchSt.On : SwitchSt.Off)).ToArray();
                        return true;
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

                return _mbClient.WriteSingle16bitRegister(address, value);
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
            var selector = write ? _startWriteAddresses : _startReadAddresses;

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
                                    ErrorCode code, string details = null) {
            try {
                _errorHistory.Push( 
                        new LogRecord( Primitives.Utility.LogLevel.Error, 
                        $"{methodName}: {ErrorCode.ConfigDeserialisationError}. " +
                        $"{details}"), force: true);
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
