using Grumpy.DAQmxNetApi;
using DAQmx = Grumpy.DAQmxNetApi.DAQmxCLIWrapper;

using Xunit.Abstractions;


namespace Grumpy.DAQmxWrapUnitTest
{
    public class QAQmxAOTestClass
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public QAQmxAOTestClass(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Test1AOSingleSample() {
            _testOutputHelper.WriteLine("TestAOLines Started.");

            IntPtr handle = DAQmxTestHelper.CreateAndConfigureAioTask(
                testOutputHelper: _testOutputHelper,
                channels: DAQmxTestHelper.AoChannels,
                taskName: DAQmxTestHelper.AoTaskName);
            int result;

            result = DAQmx.StartTask(handle);
            Assert.True(DAQmx.Success(result), DAQmx.GetErrorDescription(result));
            _testOutputHelper.WriteLine("AO task started.");

            double[] data = new double[DAQmxTestHelper.SamplesPerChannel * DAQmxTestHelper.NumberOfPhysicalChannels];
            for (int i = 0; i < data.Length; i++) {
                data[i] = i * 0.1; // Example data
            }

            result = DAQmx.WriteAnalogF64(handle, DAQmxTestHelper.SamplesPerChannel, false, DAQmxTestHelper.TimeoutS, DAQmxTestHelper.WriteFillMode, data, out int samplesWritten);
            Assert.True(DAQmx.Success(result), DAQmx.GetErrorDescription(result));
            _testOutputHelper.WriteLine($"Written {samplesWritten} samples.");

            result = DAQmx.StopTask(handle);
            Assert.True(DAQmx.Success(result), DAQmx.GetErrorDescription(result));
            _testOutputHelper.WriteLine("Task stopped.");

            result = DAQmx.DisposeTask(out handle);
            Assert.True(DAQmx.Success(result), DAQmx.GetErrorDescription(result));
            _testOutputHelper.WriteLine($"Task disposed. Handle: {string.Format("{0:X}", handle)}.");
        }

        [Fact]
        public void Test2AOFiniteSamples() {

            _testOutputHelper.WriteLine("TestAOLines Started.");

            IntPtr handle = DAQmxTestHelper.CreateAndConfigureAioTask(
                testOutputHelper: _testOutputHelper,
                channels: DAQmxTestHelper.AoChannels,
                taskName: DAQmxTestHelper.AoTaskName);
            Assert.True(handle > 0, "Failed to create task.");

            int result = DAQmx.TaskControl(handle, TaskAction.Verify);
            Assert.True(DAQmx.Success(result), $"Channel verification failed {DAQmx.GetErrorDescription(result)}");
            _testOutputHelper.WriteLine("Channel verified.");

            result = DAQmx.ConfigureTiming(handle, DAQmxTestHelper.TimingSource, DAQmxTestHelper.SamplingRate, ActiveEdge.Rising, SamplingMode.FiniteSamples, DAQmxTestHelper.FiniteSamplesPerChannel);
            Assert.True(DAQmx.Success(result), DAQmx.GetErrorDescription(result));
            _testOutputHelper.WriteLine("AO timing configured.");

            result = DAQmx.TaskControl(handle, TaskAction.Verify);
            Assert.True(DAQmx.Success(result), $"Timing verification failed {DAQmx.GetErrorDescription(result)}");
            _testOutputHelper.WriteLine("Timing verified.");

            double[] data = new double[DAQmxTestHelper.FiniteSamplesPerChannel * DAQmxTestHelper.NumberOfPhysicalChannels];

            double inc = 10.0 / data.Length;

            for (int i = 0; i < data.Length; i++) {
                data[i] = i * inc; // Example data
            }

            result = DAQmx.StartTask(handle);
            Assert.True(DAQmx.Success(result), DAQmx.GetErrorDescription(result));
            _testOutputHelper.WriteLine("AO task started.");

            result = DAQmx.WriteAnalogF64(handle, DAQmxTestHelper.FiniteSamplesPerChannel, false, DAQmxTestHelper.TimeoutS, DAQmxTestHelper.WriteFillMode, data, out int samplesWritten);
            Assert.True(DAQmx.Success(result), DAQmx.GetErrorDescription(result));
            _testOutputHelper.WriteLine($"Written {samplesWritten} samples.");

            result = DAQmx.WaitUntilTaskDone(handle, 10);
            Assert.True(DAQmx.Success(result), DAQmx.GetErrorDescription(result));
            _testOutputHelper.WriteLine("Task complete.");

            result = DAQmx.StopTask(handle);
            Assert.True(DAQmx.Success(result), DAQmx.GetErrorDescription(result));
            _testOutputHelper.WriteLine("Task stopped.");

            result = DAQmx.DisposeTask(out handle);
            Assert.True(DAQmx.Success(result), DAQmx.GetErrorDescription(result));
            _testOutputHelper.WriteLine($"Task disposed. Handle: {string.Format("{0:X}", handle)}.");
        }
    }
}