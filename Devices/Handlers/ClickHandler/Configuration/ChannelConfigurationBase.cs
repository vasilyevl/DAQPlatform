using GSE.Common;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;


namespace GSE.ClickPLCHandler
{
    public enum IOType
    {
        Unknown,
        Input,
        InputRegister,
        Output,
        OutputRegister,
        AnalogInput,
        AnalogOutput,
        ControlRelay,
        RegisterInt16,
        RegisterInt32,
        RegisterHex,
        RegisterFloat32,
        Timer,
        TimerRegister,
        Counter,
        CounterRegister,
        SystemControlRelay,
        SystemRegister,
        Text
    }


    public static class ChannelConstants
    {
        private static IReadOnlyDictionary<string, IOType> _ioTypes =
            new Dictionary<string, IOType>() {
                {"X", IOType.Input },
                {"Y", IOType.Output},
                {"C", IOType.ControlRelay},
                {"T", IOType.Timer},
                {"CT", IOType.Counter},
                {"SC", IOType.SystemControlRelay},
                {"DS", IOType.RegisterInt16},
                {"DD", IOType.RegisterInt32},
                {"DH", IOType.RegisterHex},
                {"DF", IOType.RegisterFloat32},
                {"XD", IOType.InputRegister},
                {"YD", IOType.OutputRegister},
                {"TD", IOType.TimerRegister},
                {"CTD", IOType.CounterRegister},
                {"SD", IOType.SystemRegister},
                {"TXT", IOType.Text}
            };

        public static IReadOnlyDictionary<string, IOType> IoTypes
        {
            get
            {
                var res = new Dictionary<string, IOType>();
                foreach (var kv in _ioTypes)
                {
                    res.Add((string)kv.Key.Clone(), kv.Value);
                }
                return res;
            }
        }

        public static List<string> ValidControlNamePreffixes =>
            new List<string>(_ioTypes.Keys);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ChannelConfigurationBase<T> : ConfigurationBase
    {
        public const int InvalidControlAddress = 0;

        #region Static read only members
        protected static IReadOnlyDictionary<string, IOType> _ioTypes =
            new Dictionary<string, IOType>() {
            {"X", IOType.Input },
            {"Y", IOType.Output},
            {"C", IOType.ControlRelay},
            {"T", IOType.Timer},
            {"CT", IOType.Counter},
            {"SC", IOType.SystemControlRelay},
            {"DS", IOType.RegisterInt16},
            {"DD", IOType.RegisterInt32},
            {"DH", IOType.RegisterHex},
            {"DF", IOType.RegisterFloat32},
            {"XD", IOType.InputRegister},
            {"YD", IOType.OutputRegister},
            {"TD", IOType.TimerRegister},
            {"CTD", IOType.CounterRegister},
            {"SD", IOType.SystemRegister},
            {"TXT", IOType.Text}
        };

        internal static List<string> ValidControlNamePreffixes =>
            new List<string>(_ioTypes.Keys);


        #endregion Static readonly members

        private string _controlName;
        private string _alias;
        private T _startUpValue;
        private bool _setOnConnect;

        public ChannelConfigurationBase() : base()
        {
            _alias = null;
            _controlName = null;
            _startUpValue = default;
            _setOnConnect = false;
        }

        public static bool GetErrorDescripton(int code, out string errorDescription)
        {
            if (Enum.IsDefined(typeof(ErrorCode), code))
            {

                errorDescription =
                    ClickPlcHandlerErrors.GetErrorDescription((ErrorCode)code);
                return true;
            }
            else
            {
                errorDescription = $"Code: {code} " +
                $"{ClickPlcHandlerErrors.GetErrorDescription(ErrorCode.CodeNotDefined)}";
            }

            return false;
        }

        public override bool CopyFrom(object src)
        {
            var s = src as ChannelConfigurationBase<T>;

            if (s == null) { return false; }

            try
            {
                Alias = s._alias;
                ControlName = s._controlName;
                StartUpValue = s._startUpValue;
                SetOnConnect = s._setOnConnect;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override object Clone()
        {
            var clone = new ChannelConfigurationBase<T>();
            clone.ControlName = _controlName;
            clone.Alias = _alias;
            clone.StartUpValue = _startUpValue;
            clone.SetOnConnect = _setOnConnect;
            return clone;
        }

        #region JSON Properties
        [JsonProperty]
        public string ControlName
        {
            get => (string)_controlName?.Clone() ?? string.Empty;
            set => _controlName = ((string)value?.Clone() ?? null)?.ToUpper() ?? null;
        }

        [JsonProperty]
        public string Alias
        {
            get => (string)_alias?.Clone() ?? string.Empty;
            set => _alias = (string)value?.Clone() ?? null;
        }
        public bool ShouldSerializeAlias() => !string.IsNullOrEmpty(_alias);

        [JsonProperty]
        public bool SetOnConnect
        {
            get => _setOnConnect && !IsReadOnly();
            set => _setOnConnect = value;
        }
        public bool ShouldSerialiseSetOnConnect() => !IsReadOnly();

        [JsonProperty]
        public T StartUpValue
        {
            get => _startUpValue;
            set => _startUpValue = value;
        }
        public bool ShouldSerializeStartUpValue() => !IsReadOnly();
        #endregion JSON Properties

        [JsonIgnore]
        public virtual IOType IOType
        {
            get
            {
                var preffx = _GetControlNamePreffix();
                return (string.IsNullOrEmpty(preffx) || !_ioTypes.ContainsKey(preffx)) ?
                     IOType.Unknown :
                     _ioTypes[preffx];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ControlNameIsValid() => _GetControlAddress(out int address);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool IsValid()
        {
            LastErrorCode = (int)ErrorCode.NoError;
            return ControlNameIsValid();
        }

        public virtual bool IsReadOnly()
        {
            var t = IOType;
            return t == IOType.Input ||
                   t == IOType.InputRegister ||
                   t == IOType.SystemControlRelay ||
                   t == IOType.SystemRegister;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TheSameChannel(ChannelConfigurationBase<T> other)
        {
            if (other == null) { return false; }
            return _controlName?.Equals(other._controlName) ?? false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected string _GetControlNamePreffix()
        {

            var res = ValidControlNamePreffixes.Where((x) => ControlName.StartsWith(x)).FirstOrDefault();
            if (res == null)
            {
                LastErrorCode = (int)ErrorCode.InvalidControlNamePreffix;
            }
            return res;
        }

        protected bool _GetControlAddress(out int address)
        {

            address = InvalidControlAddress;

            var pref = _GetControlNamePreffix();

            if (!string.IsNullOrEmpty(pref))
            {

                try
                {
                    address = Int32.Parse(ControlName.Substring(ControlName.IndexOf(pref), pref.Length));

                    if (address <= 0)
                    {
                        LastErrorCode = (int)ErrorCode.InvalidControlAddress;
                    }
                }
                catch
                {
                    LastErrorCode = (int)ErrorCode.InvalidControlName;
                    address = InvalidControlAddress;
                }
            }
            return address > InvalidControlAddress;
        }
    }
}
