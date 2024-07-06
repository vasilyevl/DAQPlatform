using Grumpy.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace LV.ClickPLCHandler
{
    public enum IOType {
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

    public enum ErrorCode
    {
        NoError = 0,
        CodeNotDefined = -1,
        InvalidControlName = -2,
        InvalidControlNamePreffix = -3,
        InvalidControlAddress = -4
    }



    [JsonObject(MemberSerialization.OptIn)]
    public class ChannelConfigurationBase<T> : ConfigurationBase
            where T : IEquatable<T>
    {

        public const  int InvalidControlAddress = 0;
        
        protected static IReadOnlyDictionary<string, IOType> _ioTypes = new Dictionary<string, IOType>() {

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

        internal static List<string> ValidControlNamePreffixes => new List<string>(_ioTypes.Keys);

        private static readonly IReadOnlyDictionary<ErrorCode, string> _errorDescriptors = new Dictionary<ErrorCode, string>()
        {
            { ErrorCode.NoError,  "No Error."},
            { ErrorCode.CodeNotDefined,  "Invalid error code." },
            { ErrorCode.InvalidControlName,  "Invalid control name assigned." },
            { ErrorCode.InvalidControlNamePreffix,  "Invalid control name prefix." },
            { ErrorCode.InvalidControlAddress,  "Invalid control name address ( must be 1+)." }
        };


        public ChannelConfigurationBase() : base()
        { }



        public static bool GetErrorDescription(int code, out string? errorDescription)
        {
            if (Enum.IsDefined(typeof(ErrorCode), code)) {

                var cd = (ErrorCode) code;

                errorDescription = (_errorDescriptors?.ContainsKey(cd) ?? false) ?
                    (string)_errorDescriptors[cd].Clone() : null;
                return true;
            }

            errorDescription = $"Code: {code} {_errorDescriptors[ErrorCode.CodeNotDefined].Clone()}";
            return false;
        }



        public override bool CopyFrom(object src)
        {
            var s = src as ChannelConfigurationBase<T>;

            if (s == null) { return false; }

            Alias = s.Alias;
            ControlName = s.ControlName;

            return true;
        }

        private string? _controlName;
        [JsonProperty]
        public string ControlName {
            get => (string)_controlName?.Clone()! ?? string.Empty;
            set => _controlName = ((string?)value?.Clone() ?? null)?.ToUpper() ?? null;
        }

        private string? _alias;
        [JsonProperty]
        public string Alias {
            get => (string)_alias?.Clone()! ?? string.Empty;
            set => _alias = (string?)value?.Clone() ?? null;
        }

        public T? StartUpValue {
            get;
            set;
        }
        public bool ShouldSerializeStartUpValue() => !ReadOnly();


        internal virtual bool ReadOnly()
        {
            var t = IOType;
            return t == IOType.Input || 
                   t == IOType.InputRegister ||
                   t == IOType.SystemControlRelay || 
                   t == IOType.SystemRegister;
        }

        public bool ShouldSerializeAlias() => !string.IsNullOrEmpty(_alias);

        public virtual IOType IOType
        {
            get {
                var preffx = _GetControlNamePrefix();
                return (string.IsNullOrEmpty(preffx) || !_ioTypes.ContainsKey(preffx)) ?
                     IOType.Unknown :
                     _ioTypes[preffx];
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected string _GetControlNamePrefix() {
            
                var res = ValidControlNamePreffixes.Where((x) => ControlName.StartsWith(x)).FirstOrDefault();
                if ( res == null) {
                    LastErrorCode = (int)ErrorCode.InvalidControlNamePreffix;
                }
                return res ?? string.Empty;           
        }

        protected bool _GetControlAddress(out int address) {

            address = InvalidControlAddress;

            String? prefix = _GetControlNamePrefix();

            if (!string.IsNullOrEmpty(prefix)) {

                try {
                    address = Int32.Parse(ControlName.Substring(ControlName.IndexOf(prefix), prefix.Length));

                    if (address <= 0) {
                        LastErrorCode = (int)ErrorCode.InvalidControlAddress;
                    }
                }
                catch {
                    LastErrorCode = (int)ErrorCode.InvalidControlName;
                    address = InvalidControlAddress;
                }
            }
            return address > InvalidControlAddress;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ControlNameIsValid() => _GetControlAddress(out int address);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool IsValid()
        {
            LastErrorCode = (int)ErrorCode.NoError;
            return ControlNameIsValid();
        }



        public override void Reset(){
            _alias = null;
            _controlName = null;
        }
    }

}
