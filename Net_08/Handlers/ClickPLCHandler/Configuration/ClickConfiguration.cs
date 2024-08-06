using Grumpy.Common;
using Newtonsoft.Json;


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
    public class ChannelConfigurationBase : ConfigurationBase
    {
        public const  int InvalidControlAddress = 0;
        internal static List<string> ValidControlNamePreffixes => new List<string>(_ioTypes.Keys);


        private static Dictionary<string, IOType>  _ioTypes = new Dictionary<string, IOType>() {
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

        private static readonly Dictionary<ErrorCode, string> _errorDescriptors = new Dictionary<ErrorCode, string>()
        {
            { ErrorCode.NoError,  "No Error."},
            { ErrorCode.CodeNotDefined,  "Invalid error code." },
            { ErrorCode.InvalidControlName,  "Invalid control name assigned." },
            { ErrorCode.InvalidControlNamePreffix,  "Invalid control name preffix." },
            { ErrorCode.InvalidControlAddress,  "Invalid control name addres ( must be 1+)." }
        };

        public static bool GetErrorDescripton(int code, out string errorDescription)
        {
            if (Enum.IsDefined(typeof(ErrorCode), code)) {
                var cd = (ErrorCode) code;

                errorDescription = (_errorDescriptors.ContainsKey(cd)) ?
                    (string)_errorDescriptors[cd].Clone() : null;
                return true;
            }

            errorDescription = $"{_errorDescriptors[ErrorCode.CodeNotDefined].Clone()} ({code})";
            return false;
        }


        public ChannelConfigurationBase() : base() {

            _alias = null;
            _controlName = null;
        }

        public override bool CopyFrom(object src)
        {
            var s = src as ChannelConfigurationBase;

            if (s == null) { return false; }

            Alias = s.Alias;
            ControlName = s.ControlName;

            return true;
        }


        private string _controlName;

        [JsonProperty]
        public string ControlName {
            get => (string)_controlName?.Clone() ?? string.Empty;
            set => _controlName = ((string)value?.Clone() ?? null)?.ToUpper() ?? null;
        }

        private string _alias;

        [JsonProperty]
        public string Alias {
            get => (string)_alias?.Clone() ?? string.Empty;
            set => _alias = (string)value?.Clone() ?? null;
        }

        public bool ShouldSerializeAlias() => !string.IsNullOrEmpty(_alias);

        public virtual IOType IOType
        {
            get {
                var preffx = _ControlNamePreffix;
                return (string.IsNullOrEmpty(preffx) || !_ioTypes.ContainsKey(preffx)) ?
                     IOType.Unknown :
                     _ioTypes[preffx];
            }
        }



        protected string _ControlNamePreffix {

           get {
                var res = ValidControlNamePreffixes.Where((x) => ControlName.StartsWith(x)).FirstOrDefault();
                if ( res == null) {
                    LastErrorCode = (int)ErrorCode.InvalidControlNamePreffix;
                }
                return res;
            }
            
        }

        protected bool _GetControlAddress(out int address) {

            address = InvalidControlAddress;

            var pref = _ControlNamePreffix;

            if (!string.IsNullOrEmpty(pref)) {

                try {
                    address = Int32.Parse(ControlName.Substring(ControlName.IndexOf(pref), pref.Length));

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

        public bool ControlNameIsValid() => _GetControlAddress(out int address);

        public virtual bool IsValid()
        {
            LastErrorCode = (int)ErrorCode.NoError;

            if (!ControlNameIsValid()) {

                return false;
            }

          
            return true;
        }


    }


    public class ClickConfiguration: ConfigurationBase 
    {



        public ClickConfiguration():base() 
        { 
        
        
        }



        public override bool CopyFrom(object src)
        {
            throw new NotImplementedException();
        }
    }
}
