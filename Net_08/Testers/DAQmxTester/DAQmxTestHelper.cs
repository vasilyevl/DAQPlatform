using Grumpy.DAQmxNetApi;
using DAQmx = Grumpy.DAQmxNetApi.DAQmxCLIWrapper;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Converters;

namespace Grumpy.DAQmxTester
{
    [JsonObject(MemberSerialization.OptOut)]
    public class DAQmxTestHelperConfig
    {
        private const string _DeviceName = "PCIe-6323_Sim";
        private const string _AiChannels = "ai0:1";
        private const string _AoChannels = "ao0:1";
        private const string _SingleAiChannel = "ai0";
        private const string _DiChannels = "port0/line0:3";
        private const string _DoChannels = "port0/line0:3";
        private const string _CounterChannel = "ctr0";
        private const string _CounterAssignedName = "PulseGenerator";
        private const string _StartTriger = "PFI0";
        private const ActiveEdge _StartTriggerEdge = ActiveEdge.Rising;
        private const string _NameToAssign = "";
        private const string _TimingSource = "";
        private const string _AiTaskName = "myAiTask";
        private const string _AoTaskName = "myAoTask";
        private const string _DoTaskName = "myDoTask";
        private const string _DiTaskName = "myDiTask";
        private const string _DiChannelNameToAssign = "diChannel";
        private const string _DoChannelNameToAssign = "doChannel";

        private const int _NumberOfDiChannels = 4;
        private const int _NumberOfDoChannels = 4;
        private const int _NumberOfDoSamples = 10;
        private const int _NumberOfPhysicalChannels = 2;
        private const int _SamplesPerChannel = 1;
        private const int _Runs = 5;
        private const int _ReadsPerRun = 5;
        private const int _FiniteSamplesPerChannel = 100;

        private const double _ExternalTriggerTimeoutS = 120.0;
        private const double _AOFinateTaskTimeout = 10.0;
        private const double _TimeoutS = 3.0;
        private const double _SamplingRate = 1000.0;
        private const double _VoltageRangeMax = 10.0;
        private const double _VoltageRangeMin = -10.0;

        private const AiTermination _InputTermination = AiTermination.NRSE;
        private const ReadWriteFillMode _ReadbackFillMode = ReadWriteFillMode.ByChannel;
        private const ReadWriteFillMode _WriteFillMode = ReadWriteFillMode.ByChannel;

        [JsonProperty]
        public string DeviceName { get; set; } = _DeviceName;

        [JsonProperty]
        public string AiChannels { get; set; } = _AiChannels;

        [JsonProperty]
        public string AoChannels { get; set; } = _AoChannels;

        [JsonProperty]
        public string SingleAiChannel { get; set; } = _SingleAiChannel;

        [JsonProperty]
        public string DiChannels { get; set; } = _DiChannels;

        [JsonProperty]
        public int NumberOfDiChannels { get; set; } = _NumberOfDiChannels;

        [JsonProperty]
        public string DoChannels { get; set; } = _DoChannels;

        [JsonProperty]
        public string CounterChannel { get; set; } = _CounterChannel;

        [JsonProperty]
        public string CounterAssignedName { get; set; } = _CounterAssignedName;

        [JsonProperty]
        public string StartTriger { get; set; } = _StartTriger;

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public ActiveEdge StartTriggerEdge { get; set; } = _StartTriggerEdge;

        [JsonProperty]
        public double ExternalTriggerTimeoutS { get; set; } = _ExternalTriggerTimeoutS;

        [JsonProperty]
        public double AOFinateTaskTimeout { get; set; } = _AOFinateTaskTimeout;

        [JsonProperty]
        public int NumberOfDoChannels { get; set; } = _NumberOfDoChannels;

        [JsonProperty]
        public int NumberOfDoSamples { get; set; } = _NumberOfDoSamples;

        [JsonProperty]
        public string NameToAssign { get; set; } = _NameToAssign;

        [JsonProperty]
        public int NumberOfPhysicalChannels { get; set; } = _NumberOfPhysicalChannels;

        [JsonProperty]
        public int SamplesPerChannel { get; set; } = _SamplesPerChannel;

        [JsonProperty]
        public double TimeoutS { get; set; } = _TimeoutS;

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public AiTermination InputTermination { get; set; } = _InputTermination;

        [JsonProperty]
        public string TimingSource { get; set; } = _TimingSource;

        [JsonProperty]
        public double SamplingRate { get; set; } = _SamplingRate;

        [JsonProperty]
        public int Runs { get; set; } = _Runs;

        [JsonProperty]
        public int ReadsPerRun { get; set; } = _ReadsPerRun;

        [JsonProperty]
        public int FiniteSamplesPerChannel { get; set; } = _FiniteSamplesPerChannel;

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public ReadWriteFillMode ReadbackFillMode { get; set; } = _ReadbackFillMode;

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public ReadWriteFillMode WriteFillMode { get; set; } = _WriteFillMode;

        [JsonProperty]
        public string AiTaskName { get; set; } = _AiTaskName;

        [JsonProperty]
        public string AoTaskName { get; set; } = _AoTaskName;

        [JsonProperty]
        public string DoTaskName { get; set; } = _DoTaskName;

        [JsonProperty]
        public double VoltageRangeMax { get; set; } = _VoltageRangeMax;

        [JsonProperty]
        public double VoltageRangeMin { get; set; } = _VoltageRangeMin;

        [JsonProperty]
        public string DiTaskName { get; set; } = _DiTaskName;

        [JsonProperty]
        public string DiChannelNameToAssign { get; set; } = _DiChannelNameToAssign;

        [JsonProperty]
        public string DoChannelNameToAssign { get; set; } = _DoChannelNameToAssign;
    }

    public class DAQmxTestHelper
    {
        private readonly DAQmxTestHelperConfig _config;

        public DAQmxTestHelper(DAQmxTestHelperConfig? config = null) {
            _config = config ?? new DAQmxTestHelperConfig();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GenerateChannelName(string channelName, string? deviceName = null) {
            deviceName ??= _config.DeviceName;
            return $"{deviceName}/{channelName}";
        }

        public  DAQmxTestHelperConfig Config => _config;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr CreateAndConfigureAioTask(
            string? taskName = null,
            string? deviceName = null,
            string? channels = null,
            double? minVoltage = null,
            double? maxVoltage = null) {
            taskName ??= _config.AiTaskName;
            deviceName ??= _config.DeviceName;
            channels ??= _config.AiChannels;
            minVoltage ??= _config.VoltageRangeMin;
            maxVoltage ??= _config.VoltageRangeMax;

            Console.WriteLine("Creating a task...");
            IntPtr handle = IntPtr.Zero;
            int result = DAQmx.CreateTask(taskName, out handle);

            Assert(DAQmx.Success(result),
                $"Failed to create task \"{taskName}\". {DAQmx.GetErrorDescription(result)}.");

            Console.WriteLine($"Task created. Handle: {string.Format("{0:X}", handle)}.");

            if (channels.Contains("ai")) {
                result = DAQmx.CreateAIVoltageChannel(handle,
                    $"{deviceName}/{channels}",
                    string.Empty,
                    _config.InputTermination,
                    minVoltage.Value,
                    maxVoltage.Value,
                    VoltageUnits.Volts,
                    null);
            }
            else {
                result = DAQmx.CreateAOVoltageChannel(handle,
                    $"{deviceName}/{channels}",
                    string.Empty,
                    minVoltage.Value,
                    maxVoltage.Value,
                    VoltageUnits.Volts,
                    null);
            }

            Assert(DAQmx.Success(result),
                $"Channel creation failed. {DAQmx.GetErrorDescription(result)}");

            Console.WriteLine($"Channel(s) created for {channels}.");

            return handle;
        }

        internal static void Assert(bool condition, string message) {
            if (!condition) {
                throw new Exception($"Assertion failed: {message}");
            }
        }
    }
}
