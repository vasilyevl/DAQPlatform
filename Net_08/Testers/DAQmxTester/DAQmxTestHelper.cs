using Grumpy.DAQmxNetApi;
using DAQmx = Grumpy.DAQmxNetApi.DAQmxCLIWrapper;

using System.Runtime.CompilerServices;

namespace Grumpy.DAQmxTester
{
    internal static class DAQmxTestHelper
    {
        public const string DeviceName = "PCIe-6323_Sim";//"TestDevice";
        public const string AiChannels = "ai0:1";
        public const string AoChannels = "ao0:1";
        public const string NameToAssign = "";
        public const int NumberOfPhysicalChannels = 2;
        public const int SamplesPerChannel = 1;
        public const double TimeoutS = 3.0;
        public const AiTermination InputTermination = AiTermination.NRSE;
        public const string TimingSource = "";
        public const double SamplingRate = 1000.0;
        public const int Runs = 5;
        public const int ReadsPerRun = 5;
        public const int FiniteSamplesPerChannel = 100;
        public const ReadWriteFillMode ReadbackFillMode = ReadWriteFillMode.ByChannel;
        public const ReadWriteFillMode WriteFillMode = ReadWriteFillMode.ByChannel;
        public const string AiTaskName = "myAiTask";
        public const string AoTaskName = "myAoTask";
        public const double VoltageRangeMax = 10.0;
        public const double VoltageRangeMin = -10.0;
        public const string DiTaskName = "myDiTask";
        public const string DiChannels = "port0/line0:3";
        public const string DiChannelNameToAssign = "diChannel";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GenerateChannelName(
            string channelName,
            string deviceName = DeviceName) => $"{deviceName}/{channelName}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr CreateAndConfigureAioTask(
            string taskName = DAQmxTestHelper.AiTaskName,
            string deviceName = DAQmxTestHelper.DeviceName,
            string channels = DAQmxTestHelper.AiChannels,
            double minVoltage = DAQmxTestHelper.VoltageRangeMin,
            double maxVoltagw = DAQmxTestHelper.VoltageRangeMax) {

            Console.WriteLine("Creating a task...");
            IntPtr handle = IntPtr.Zero;
            int result = DAQmx.CreateTask(taskName, out handle);

            Assert(DAQmx.Success(result),
                $"Failed to create task \"{DAQmxTestHelper.AiTaskName}\". " +
                $"{DAQmx.GetErrorDescription(result)}.");

            Console.WriteLine($"Task created. " +
                    $"Handle: {string.Format("{0:X}", handle)}.");

            if (channels.Contains("ai")) {
                result = DAQmx.CreateAIVoltageChannel(handle,
                    $"{deviceName}/{channels}",
                    string.Empty,
                    DAQmxTestHelper.InputTermination,
                    minVoltage,
                    maxVoltagw,
                    VoltageUnits.Volts,
                    null);
            }
            else {
                result = DAQmx.CreateAOVoltageChannel(handle,
                    $"{deviceName}/{channels}",
                    string.Empty,
                    minVoltage,
                    maxVoltagw,
                    VoltageUnits.Volts,
                    null);

            }
            Assert(DAQmx.Success(result),
            $"Channel creation failed. " +
            $"{DAQmx.GetErrorDescription(result)}");

            Console.WriteLine($"Channel(s) " +
                $"created for {channels}.");

            return handle;
        }

        internal static void Assert(bool v, string v1) {
            
            if (!v) {

                throw new Exception($"Assertion failed; {v1}");
            }
        }
    }
}
