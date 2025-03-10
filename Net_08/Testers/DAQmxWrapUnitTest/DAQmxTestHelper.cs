using Grumpy.DAQmxNetApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;
using DAQmx = Grumpy.DAQmxNetApi.DAQmxCLIWrapper;


namespace Grumpy.DAQmxWrapUnitTest
{
    internal static class DAQmxTestHelper
    {
        public const string DeviceName = "PCIe-6323_Sim";//"TestDevice";
        public const string AiChannels = "ai0:1";
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
        public const ReadbacklFillMode ReadbackFillMode = ReadbacklFillMode.ByChannel;
        public const string AiTaskName = "myAiTask";
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
        public static  IntPtr CreateAndConfigureAioTask(
            string taskName = DAQmxTestHelper.AiTaskName,
            string deviceName = DAQmxTestHelper.DeviceName,
            string aiChannels = DAQmxTestHelper.AiChannels,
            double minVoltage = DAQmxTestHelper.VoltageRangeMin,
            double maxVoltagw = DAQmxTestHelper.VoltageRangeMax,
            ITestOutputHelper? testOutputHelper = null) {

            testOutputHelper?.WriteLine("Creating a task...");
            IntPtr handle = IntPtr.Zero;
            int result = DAQmx.CreateTask(taskName, out handle);
            Assert.True(DAQmx.Success(result),
                $"Failed to create task \"{DAQmxTestHelper.AiTaskName}\". " +
                $"{DAQmx.GetErrorDescription(result)}.");

            testOutputHelper?.WriteLine($"Task created. " +
                    $"Handle: {string.Format("{0:X}", handle)}.");

            result = DAQmx.CreateAIVoltageChannel(handle,
                $"{deviceName}/{aiChannels}",
                string.Empty,
                DAQmxTestHelper.InputTermination,
                minVoltage,
                maxVoltagw,
                VoltageUnits.Volts,
                null);

            Assert.True(DAQmx.Success(result),
                $"Channel creation failed. " +
                $"{DAQmx.GetErrorDescription(result)}");

            testOutputHelper?.WriteLine($"AI Channel(s) " +
                $"created for {aiChannels}.");

            return handle;
        }
    }
}
