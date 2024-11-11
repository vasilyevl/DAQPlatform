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

using Grumpy.DAQFramework.Configuration;
using Grumpy.DAQmxNetApi;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Grumpy.DAQmxDeviceServer.Configuration
{
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IOTypes
    {
        NA = 0,
        AnalogInput = 1,
        AnalogOutput = 2,
        DigitalInput = 4,
        DigitalOutput = 8,
        FrequencyOutput = 16,
        Pulse = 32,
        Counter = 64,
        Analog = AnalogInput | AnalogOutput,
        Digital = DigitalInput | DigitalOutput,
        IO = Analog | Digital,
        PulseCounter = Pulse | Counter
    }

    public class ChannelConfigurationBase : ConfigurationBase,
                IEquatable<ChannelConfigurationBase>
    {
        public const char ChannelNamePartsSeparator = '/';

        private string _alias;
        private string _physicalChannel;
        private string _pulseCounter;

        private IOTypes _type;
        private IOModes[] _operationModes;        
        private AiTermination _aiTermination;
        private DODrive _doDrive;

        private AIORange? _range;

        public ChannelConfigurationBase() {

            _type = IOTypes.NA;
            _aiTermination = AiTermination.Default;
            _doDrive = DODrive.Any;

            _alias = string.Empty;
            _physicalChannel = string.Empty;
            _pulseCounter = string.Empty;

            _operationModes = [];
            _range = null;        
        }

        public ChannelConfigurationBase(IOTypes type = IOTypes.NA):this() {
            
            Type = type;
        }

        [JsonProperty]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public IOTypes Type {
            get => _type;
            private set => _type = value;
        }


        [JsonProperty]
        public string Alias { 
            get => string.IsNullOrEmpty(_alias) ? _physicalChannel : _alias; 
            set => _alias = value is null? string.Empty : value; 
        }

        [JsonProperty]
        public string PhysicalChannel { 
            get => _physicalChannel; 
            set => _physicalChannel = value is null ? string.Empty: value; }

        
        [JsonProperty]
        public IOModes[] OperationModes {

            get => _operationModes == null ?[] : _operationModes;
            set => _operationModes = value ?? [];
        }

        [JsonProperty]
        public AIORange? Range { 
            get => ShouldSerializeRange() ? _range: null;
            set {
                    if (value is not null) {
                        _range = value;
                    }
            }
        }       
        public bool ShouldSerializeRange() => 
                            (Type & ( IOTypes.Analog)) != 0 ;

        [JsonProperty]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public AiTermination AITermination {

            get => ShouldSerializeAITermination() ? 
                        _aiTermination : AiTermination.Default; 
            set => _aiTermination = value; 
        }

        public bool ShouldSerializeAITermination() => 
                        (Type & IOTypes.AnalogInput) != 0;

        [JsonProperty]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public DODrive Drive { 
            get => ShouldSerializeDrive() ? DODrive.Any : _doDrive; 
            set => _doDrive = value; 
        }

        public bool ShouldSerializeDrive() =>
                        (Type & IOTypes.DigitalOutput) != 0;


        [JsonProperty]
        public string PulseCounter {

            get => ShouldSerializePulseCounter() ? 
                        string.Empty : _pulseCounter;
            set => _pulseCounter = 
                        string.IsNullOrEmpty(value) ? string.Empty :value;
        }

        public bool ShouldSerializePulseCounter() =>
            (Type & IOTypes.PulseCounter) != 0;

        [JsonIgnore]
        public IOMode RequestedModesOfOperation {

            get {

                var mode = new IOMode();

                _operationModes?.ToList().ForEach((x) => mode.Mode |= x);

                return mode;      
            }
            set {

                _operationModes = value?.ToArray() ?? [];
            }
        }

        [JsonIgnore]
        public string DeviceName {
            get {

                if (string.IsNullOrEmpty(PhysicalChannel)) {

                    return string.Empty;
                }

                string[] parts = 
                    PhysicalChannel.Split(ChannelNamePartsSeparator);

                return parts.Length > 0 ? parts[0] : string.Empty;
            }
        }

        [JsonIgnore]
        public string PhysicalChannelName {
            get {

                if (string.IsNullOrEmpty(PhysicalChannel)) {
                    
                    return string.Empty;
                }

                string[] parts = PhysicalChannel.Split('/');

                return parts.Length > 2 ? string.Concat( parts, '/'): 
                    parts.Length > 1 ? parts[1] : string.Empty;
            }
        }

        // Implicit conversion from DAQmxTaskMode to IOMode
        public static implicit operator 
            ChannelConfigurationBase(IOTypes type) =>
                new ChannelConfigurationBase(type);

        // Implicit conversion from DAQmxTaskModeWrapper to
        // DAQmxTaskMode
        public static implicit operator 
            IOTypes(ChannelConfigurationBase wrapper) =>
                wrapper.Type;

        // Method to assign an enum value to an instance of
        // DAQmxTaskModeWrapper
        public void SetType(IOTypes type) => Type = type;

        // Equals method for IEquatable<DAQmxTaskModeWrapper>
        public bool Equals(ChannelConfigurationBase? other) =>
            other != null 
                && Type == other.Type
                && (string.Compare(
                    PhysicalChannelName, 
                    other.PhysicalChannelName, 
                    ignoreCase:true) == 0)
                && (string.Compare(Alias, other.Alias) == 0)
                && RequestedModesOfOperation == other.RequestedModesOfOperation
                && (string.Compare(PulseCounter, other.PulseCounter) == 0)
                && (Range?.Equals(other.Range) ?? true)
                && AITermination == other.AITermination
                && Drive == other.Drive;

        public override bool Equals(object? obj) =>
            obj is ChannelConfigurationBase other && Equals(other);

        public override int GetHashCode() => Type.GetHashCode();

        public override string ToString() => 
            this.SerializeToString(out string? error);
    }

    public class AIORange:ConfigurationBase, IEquatable<AIORange>
    {
        private const double _epsilon = 1/0e-7;
        private double _min;
        private double _max;

        public AIORange() {

            _min = 0.0;
            _max = 0.0;
        }

        public AIORange(double min, double max) {

            _min = min;
            _max = max;
        }

        [JsonProperty]
        public double Min {

            get => _min;
            set => _min = value;
        }

        [JsonProperty]
        public double Max {

            get => _max;
            set => _max = value;
        }

        public bool Equals(AIORange? other) =>
            other != null
            && Math.Abs(Min - other.Min) < _epsilon 
            && Math.Abs(Max - other.Max) < _epsilon;

        public override bool Equals(object? obj) =>
            obj is AIORange other && Equals(other);

        public override int GetHashCode() => 
            Min.GetHashCode() ^ Max.GetHashCode();

        public override string ToString() => 
            this.SerializeToString(out string? error);
    }
}