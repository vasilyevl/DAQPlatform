using Grumpy.DAQmxNetApi;
using DAQmx = Grumpy.DAQmxNetApi.DAQmxCLIWrapper;
using Xunit.Abstractions;

using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;


namespace Grumpy.DAQmxWrapUnitTest
{
    public class QAQmxAITestClass
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public QAQmxAITestClass(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Test1AISingleSamplesSoftwareTrigger() {

            
            _testOutputHelper.WriteLine("TestAILines Started.");

            double[] data = new double[DAQmxTestHelper.SamplesPerChannel * 
                DAQmxTestHelper.NumberOfPhysicalChannels];
            IntPtr handle = DAQmxTestHelper.CreateAndConfigureAioTask(testOutputHelper: _testOutputHelper);
            int result;

            for (int rn = 0; rn < DAQmxTestHelper.Runs; rn++) {

                _testOutputHelper.WriteLine($"\n\nRun {rn+1} out of " +
                    $"{DAQmxTestHelper.Runs}.");
                
                result = DAQmx.StartTask(handle);
                Assert.True(DAQmx.Success(result), 
                    DAQmx.GetErrorDescription(result));

                _testOutputHelper.WriteLine($"AI task  started.\n");
         
                for (int rd = 0; rd < DAQmxTestHelper.ReadsPerRun; rd++) {
                
                    _testOutputHelper.WriteLine($"Read {rd + 1} out of " +
                        $"{DAQmxTestHelper.ReadsPerRun}.");

                    _testOutputHelper.WriteLine("Reading data using " +
                        "ReadAnalogF64");

                    result = DAQmx.ReadAnalogF64(handle, 
                        DAQmxTestHelper.SamplesPerChannel,
                        DAQmxTestHelper.TimeoutS, 
                        DAQmxTestHelper.ReadbackFillMode, 
                        data,
                        out int samplesRead);

                    Assert.True(DAQmx.Success(result),
                                              DAQmx.GetErrorDescription(result));

                    StringBuilder sb = new StringBuilder();

                    sb.Append("Sample");

                    for (int ch = 0; ch < DAQmxTestHelper.NumberOfPhysicalChannels; ch++) {

                        sb.Append($"\tChannel {ch}");
                    }
                    sb.Append("\n");

                    for (int i = 0; i < samplesRead; i++) {

                        sb.Append($" {i + 1} ");
                        for (int ch = 0; ch < DAQmxTestHelper.NumberOfPhysicalChannels; ch++) {

                            sb.Append($"\t\t{data[i * DAQmxTestHelper.NumberOfPhysicalChannels + ch]:F2}.");
                        }

                        sb.Append("\n");
                    }

                    _testOutputHelper.WriteLine($"Read {samplesRead} samples " +
                        $"out of {DAQmxTestHelper.SamplesPerChannel} requested.\n" +
                        $"{sb.ToString()}");
                }

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

            double[] data = new double[DAQmxTestHelper.FiniteSamplesPerChannel * 
                DAQmxTestHelper.NumberOfPhysicalChannels];
            
            _testOutputHelper.WriteLine("TestAILines Started.");

            IntPtr handle = DAQmxTestHelper.CreateAndConfigureAioTask(
                testOutputHelper: _testOutputHelper);
     
            Assert.True(handle > 0, "Failed to create task.");

            _testOutputHelper.WriteLine($"Task created. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            int result = DAQmx.TaskControl(handle, TaskAction.Verify);

            Assert.True(DAQmx.Success(result),
                    $"Channel verification failed " +
                    $"{DAQmx.GetErrorDescription(result)}");

            _testOutputHelper.WriteLine($"Channel verified.");

            result = DAQmx.ConfigureTiming(handle, 
                DAQmxTestHelper.TimingSource,
                DAQmxTestHelper.SamplingRate, 
                ActiveEdge.Rising,
                SamplingMode.FiniteSamples, 
                DAQmxTestHelper.FiniteSamplesPerChannel);

            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            result = DAQmx.TaskControl(handle, TaskAction.Verify);
            _testOutputHelper.WriteLine($"AI timing  configured.");

            Assert.True(DAQmx.Success(result),
                    $"Timing verification failed " +
                    $"{DAQmx.GetErrorDescription(result)}");

            _testOutputHelper.WriteLine($"Timing verified.");


            StartTime = DateTime.Now;
            result = DAQmx.StartTask(handle);
            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"AI task  started.");

            _testOutputHelper.WriteLine("Reading data using ReadAnalogF64");
            
            result = DAQmx.WaitUntilTaskDone(handle, 10);

            EndTime = DateTime.Now;
            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));
            _testOutputHelper.WriteLine($"Task complete in " +
                $"{(EndTime - StartTime).TotalMilliseconds}ms.");

            result = DAQmx.ReadAnalogF64(handle, 
                DAQmxTestHelper.FiniteSamplesPerChannel,
                DAQmxTestHelper.TimeoutS, 
                DAQmxTestHelper.ReadbackFillMode, 
                data,
                out int samplesRead);

            Assert.True(DAQmx.Success(result),
                 DAQmx.GetErrorDescription(result));

            StringBuilder sb = new StringBuilder();
            sb.Append("Sample");

            for (int ch = 0; ch < DAQmxTestHelper.NumberOfPhysicalChannels; ch++) {
                sb.Append($"\tChannel {ch}");
            }

            sb.Append("\n");

            for (int i = 0; i < samplesRead; i++) {

                sb.Append($"  {i + 1} ");
          
                for (int ch = 0; ch < DAQmxTestHelper.NumberOfPhysicalChannels; ch++) {

                    sb.Append($"\t\t{data[i * DAQmxTestHelper.NumberOfPhysicalChannels + ch]:F2}.");
                }

                sb.Append("\n");
            }

            _testOutputHelper.WriteLine($"Read {samplesRead} samples " +
                $"out of {DAQmxTestHelper.FiniteSamplesPerChannel} requested.\n" +
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

        public int EventCallback(IntPtr taskHandle, 
            int status, 
            ref IntPtr callbackData) {
            EndTime = DateTime.Now;

            _testOutputHelper.WriteLine($"Event callback called " +
                $"with status {status}. Time laps: " +
                $"{(EndTime - StartTime).TotalMilliseconds}ms.");
            
            return 0;
        }

        static DateTime StartTime;
        static DateTime EndTime;


        [Fact]
        public void Test3AIFiniteSamplesSoftwareTriggerWEvent() {

            double[] data = new double[DAQmxTestHelper.FiniteSamplesPerChannel * 
                DAQmxTestHelper.NumberOfPhysicalChannels];

            _testOutputHelper.WriteLine("TestAILines Started.");

            _testOutputHelper.WriteLine("Creating a task...");

            IntPtr handle = DAQmxTestHelper.CreateAndConfigureAioTask(
                testOutputHelper: _testOutputHelper);

            Assert.True(handle > 0, "Failed to create task.");

            int result = DAQmx.TaskControl(handle, TaskAction.Verify);

            Assert.True(DAQmx.Success(result),
                    $"Channel verification failed " +
                    $"{DAQmx.GetErrorDescription(result)}");

            _testOutputHelper.WriteLine($"Channel verified.");

            result = DAQmx.ConfigureTiming(handle, 
                DAQmxTestHelper.TimingSource,
                DAQmxTestHelper.SamplingRate, 
                ActiveEdge.Rising,
                SamplingMode.FiniteSamples, 
                DAQmxTestHelper.FiniteSamplesPerChannel);

            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            result = DAQmx.TaskControl(handle, TaskAction.Verify);
            _testOutputHelper.WriteLine($"AI timing  configured.");

            Assert.True(DAQmx.Success(result),
                    $"Timing verification failed " +
                    $"{DAQmx.GetErrorDescription(result)}");

            _testOutputHelper.WriteLine($"Timing verified.");

            DAQmxDoneCallbackDelegate cbhandle = 
                new DAQmxDoneCallbackDelegate(EventCallback);

            CallbackHandle callbackHandler = 
                CallbackService.RegisterDoneEvent(handle, cbhandle, this);

            StartTime = DateTime.Now;
            result = DAQmx.StartTask(handle);
            Assert.True(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"AI task  started.");

            _testOutputHelper.WriteLine("Reading data using ReadAnalogF64");

            result = DAQmx.ReadAnalogF64(handle, 
                DAQmxTestHelper.FiniteSamplesPerChannel,
                DAQmxTestHelper.TimeoutS, 
                DAQmxTestHelper.ReadbackFillMode, data,
                out int samplesRead);

            Assert.True(DAQmx.Success(result),
                 DAQmx.GetErrorDescription(result));

            StringBuilder sb = new StringBuilder();
            
            sb.Append("Sample");
            
            for (int ch = 0; ch < DAQmxTestHelper.NumberOfPhysicalChannels; ch++) {
                sb.Append($"\tChannel {ch}");
            }

            sb.Append("\n");

            for (int i = 0; i < samplesRead; i++) {

                sb.Append($"  {i + 1} ");
                for (int ch = 0; 
                    ch < DAQmxTestHelper.NumberOfPhysicalChannels; 
                    ch++) {

                    sb.Append(
                        $"\t\t{data[i * DAQmxTestHelper.NumberOfPhysicalChannels + ch]:F2}.");
                }
                sb.Append("\n");
            }

            _testOutputHelper.WriteLine(
                $"Read {samplesRead} samples " +
                $"out of {DAQmxTestHelper.FiniteSamplesPerChannel} " +
                $"requested.\n{sb.ToString()}");

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
