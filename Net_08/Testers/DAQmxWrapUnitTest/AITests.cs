using Grumpy.DAQmxCLIWrap;
using DAQmx = Grumpy.DAQmxCLIWrap.DAQmxCLIWrapper;
using Xunit.Abstractions;

using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;


namespace Grumpy.DAQmxWrapUnitTest
{
    public class QAQmxAITestClass
    {

        private readonly ITestOutputHelper _testOutputHelper;
        private string deviceName = "Dev1";//"TestDevice";
        private string aiChannels = "ai0:1";
        private string nameToAssign = "";
        private int physicalChannels = 2;
        private int samplesPerChannel = 1;
        private double timeoutS = 3.0;
        private AiTermination inputTermination = AiTermination.NRSE;
        private string timingSource = "";
        private double samplingRate = 1000.0;
        private int runs = 25; 
        private int finiteSamplesPerChannel = 100;
        private ReadbacklFillMode readbackFillMode = ReadbacklFillMode.ByChannel;

        
        public QAQmxAITestClass(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Test1AISingleSamplesSoftwareTrigger() {

            double[] data = new double[samplesPerChannel * physicalChannels];

            _testOutputHelper.WriteLine("TestAILines Started.");
            IntPtr handle = IntPtr.Zero;

            _testOutputHelper.WriteLine("Creating a task...");

            Int32 result = DAQmx.CreateTask("myAiTask", out handle);
            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"Task created. " +
                $"Handle: {string.Format("{0:X}", handle)}.");

            result = DAQmx.CreateAIVoltageChannel(handle, 
                $"{deviceName}/{aiChannels}", nameToAssign, inputTermination, 
                -10.0, 10.0, VoltageUnits.Volts, null);

            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"AI Channel(s) " +
                $"created for {aiChannels}.");

            for (int r = 0; r < runs; r++) {

                result = DAQmx.StartTask(handle);
                Assert.True(DAQmx.Success(result),
                    DAQmx.GetErrorDescription(result));

                _testOutputHelper.WriteLine($"AI task  started.");


                _testOutputHelper.WriteLine("Reading data using ReadAnalogF64");

                result = DAQmx.ReadAnalogF64(handle, samplesPerChannel,
                    timeoutS, readbackFillMode, data,
                    out int samplesRead);

                Assert.True(DAQmx.Success(result),
                                          DAQmx.GetErrorDescription(result));

                StringBuilder sb = new StringBuilder();
                sb.Append("Sample");

                for (int ch = 0; ch < physicalChannels; ch++) {

                    sb.Append($"\tChannel {ch}");
                }
                sb.Append("\n");

                for (int i = 0; i < samplesRead; i++) {

                    sb.Append($" {i + 1} ");
                    for (int ch = 0; ch < physicalChannels; ch++) {

                        sb.Append($"\t\t{data[i * physicalChannels + ch]:F2}.");
                    }

                    sb.Append("\n");
                }

                _testOutputHelper.WriteLine($"Read {samplesRead} samples " +
                    $"out of {samplesPerChannel} requested.\n" +
                    $"{sb.ToString()}");

                result = DAQmx.IsTaskDone(handle, out bool isDone);

                Assert.True(DAQmx.Success(result),
                    DAQmx.GetErrorDescription(result));

                _testOutputHelper.WriteLine($"Task is done: {isDone}.");

                result = DAQmx.StopTask(handle);

                Assert.True(DAQmx.Success(result),
                    DAQmx.GetErrorDescription(result));

                _testOutputHelper.WriteLine($"Task stopped.");
            }

            result = DAQmx.DisposeTask(out handle);

            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"Task disposed. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            _testOutputHelper.WriteLine(" Analog read complete");
        }

        [Fact]
        public void Test2AIFiniteSamplesSoftwareTrigger() {

            double[] data = new double[finiteSamplesPerChannel * physicalChannels];
            
            _testOutputHelper.WriteLine("TestAILines Started.");
            IntPtr handle = IntPtr.Zero;

            _testOutputHelper.WriteLine("Creating a task...");

            Int32 result = DAQmx.CreateTask("myAiTask", out handle);

            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"Task created. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            result = DAQmx.CreateAIVoltageChannel(handle,
                $"{deviceName}/{aiChannels}", nameToAssign, inputTermination,
                -10.0, 10.0, VoltageUnits.Volts, null);

            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"AI Channel(s) created for." +
                $" {deviceName}/{aiChannels}");

            result = DAQmx.TaskControl(handle, TaskAction.Verify);

            Assert.True(DAQmx.Success(result),
                    $"Channel verification failed {DAQmx.GetErrorDescription(result)}");

            _testOutputHelper.WriteLine($"Channel verified.");


            result = DAQmx.ConfigureTiming(handle, timingSource,
                samplingRate, ActiveEdge.Rising,
                SamplingMode.FiniteSamples, finiteSamplesPerChannel);

            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            result = DAQmx.TaskControl(handle, TaskAction.Verify);
            _testOutputHelper.WriteLine($"AI timing  configured.");

            Assert.True(DAQmx.Success(result),
                    $"Timing verification failed {DAQmx.GetErrorDescription(result)}");

            _testOutputHelper.WriteLine($"Timing verified.");



            result = DAQmx.StartTask(handle);
            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"AI task  started.");

            _testOutputHelper.WriteLine("Reading data using ReadAnalogF64");

            result = DAQmx.ReadAnalogF64(handle, finiteSamplesPerChannel,
                timeoutS, readbackFillMode, data,
                out int samplesRead);

            Assert.True(DAQmx.Success(result),
                 DAQmx.GetErrorDescription(result));

            StringBuilder sb = new StringBuilder();
            sb.Append("Sample");
            for (int ch = 0; ch < physicalChannels; ch++) {
                sb.Append($"\tChannel {ch}");
            }
            sb.Append("\n");

            for (int i = 0; i < samplesRead; i++) {

                sb.Append($"  {i + 1} ");
                for (int ch = 0; ch < physicalChannels; ch++) {

                    sb.Append($"\t\t{data[i * physicalChannels + ch]:F2}.");
                }
                sb.Append("\n");
            }

            _testOutputHelper.WriteLine($"Read {samplesRead} samples " +
                $"out of {finiteSamplesPerChannel} requested.\n" +
                $"{sb.ToString()}");

            result = DAQmx.IsTaskDone(handle, out bool isDone);

            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"Task is done: {isDone}.");

            result = DAQmx.StopTask(handle);

            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"Task stopped.");

            result = DAQmx.DisposeTask(out handle);

            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"Task disposed. " +
                $"Handle: {string.Format("{0:X}", handle)}.");

            _testOutputHelper.WriteLine(" Analog read complete");
        }
    }
}
