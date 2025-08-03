using System;
using System.Text;
using DAQmxTester;
using Grumpy.DAQmxNetApi;
using DAQmx = Grumpy.DAQmxNetApi.DAQmxCLIWrapper;

namespace Grumpy.DAQmxTester
{
    public class DAQmxAITestClass: DAQmxTestBase
    {
        private DAQmxTestHelper _deviceHelper;

        public DAQmxAITestClass(DAQmxTestHelperConfig? config = null ) {
            
            _deviceHelper = new DAQmxTestHelper(config);
        }


        public void AITestSingleSamplesSoftwareTrigger() {
            Console.WriteLine("TestAILines Started.");

            double[] data = new double[_helper.Config.SamplesPerChannel *
                _helper.Config.NumberOfPhysicalChannels];
            
            IntPtr handle = _helper.CreateAndConfigureAioTask();
            int result;

            for (int rn = 0; rn < _helper.Config.Runs; rn++) {
                Console.WriteLine($"\n\nRun {rn + 1} out of {_helper.Config.Runs}.");

                result = DAQmx.StartTask(handle);
                if (!DAQmx.Success(result)) {
                    Console.WriteLine(DAQmx.GetErrorDescription(result));
                    return;
                }

                Console.WriteLine("AI task started.\n");

                for (int rd = 0; rd < _helper.Config.ReadsPerRun; rd++) {
                    
                    Console.WriteLine($"Read {rd + 1} out of {_helper.Config.ReadsPerRun}.");
                    Console.WriteLine("Reading data using ReadAnalogF64");

                    result = DAQmx.ReadAnalogLines(handle, _helper.Config.SamplesPerChannel, 
                        _helper.Config.TimeoutS, _helper.Config.ReadbackFillMode, 
                        data, out int samplesRead);
                    
                    if (!DAQmx.Success(result)) {
                        Console.WriteLine(DAQmx.GetErrorDescription(result));
                        return;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append("Sample");

                    for (int ch = 0; ch < _helper.Config.NumberOfPhysicalChannels; ch++) {
                        sb.Append($"\tChannel {ch}");
                    }
                    sb.Append("\n");

                    for (int i = 0; i < samplesRead; i++) {
                        sb.Append($" {i + 1} ");
                        for (int ch = 0; ch < _helper.Config.NumberOfPhysicalChannels; ch++) {
                            sb.Append($"\t\t{data[i * _helper.Config.NumberOfPhysicalChannels + ch]:F2}.");
                        }
                        sb.Append("\n");
                    }

                    Console.WriteLine($"Read {samplesRead} samples out of {_helper.Config.SamplesPerChannel} requested.\n{sb.ToString()}");
                }

                result = DAQmx.IsTaskDone(handle, out bool isDone);
                if (!DAQmx.Success(result)) {
                    Console.WriteLine(DAQmx.GetErrorDescription(result));
                    return;
                }

                Console.WriteLine($"Task is done: {isDone}.");

                result = DAQmx.StopTask(handle);
                if (!DAQmx.Success(result)) {
                    Console.WriteLine(DAQmx.GetErrorDescription(result));
                    return;
                }

                Console.WriteLine("Task stopped.");
            }

            result = DAQmx.DisposeTask(out handle);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            Console.WriteLine($"Task disposed. Handle: {string.Format("{0:X}", handle)}.");
            Console.WriteLine("Analog read complete");
        }

        public void AITestFiniteSamplesSoftwareTrigger() {

            double[] data = new double[_helper.Config.FiniteSamplesPerChannel * 
                _helper.Config.NumberOfPhysicalChannels];
            
            Console.WriteLine("TestAILines Started.");

            IntPtr handle = _helper.CreateAndConfigureAioTask();
            
            if (handle == IntPtr.Zero) {
            
                Console.WriteLine("Failed to create task.");
                return;
            }

            Console.WriteLine($"Task created. Handle: {string.Format("{0:X}", handle)}.");

            int result = DAQmx.TaskControl(handle, TaskAction.Verify);
            if (!DAQmx.Success(result)) {
                Console.WriteLine($"Channel verification failed {DAQmx.GetErrorDescription(result)}");
                return;
            }

            Console.WriteLine("Channel verified.");

            result = DAQmx.ConfigureTiming(handle, _helper.Config.TimingSource, 
                _helper.Config.SamplingRate, ActiveEdge.Rising, 
                SamplingMode.FiniteSamples, _helper.Config.FiniteSamplesPerChannel);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            result = DAQmx.TaskControl(handle, TaskAction.Verify);
            if (!DAQmx.Success(result)) {
                Console.WriteLine($"Timing verification failed {DAQmx.GetErrorDescription(result)}");
                return;
            }

            Console.WriteLine("Timing verified.");

            StartTime = DateTime.Now;
            result = DAQmx.StartTask(handle);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            Console.WriteLine("AI task started.");
            Console.WriteLine("Reading data using ReadAnalogF64");

            result = DAQmx.WaitUntilTaskDone(handle, 10);
            EndTime = DateTime.Now;
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            Console.WriteLine($"Task complete in {(EndTime - StartTime).TotalMilliseconds}ms.");

            result = DAQmx.ReadAnalogLines(handle, _helper.Config.FiniteSamplesPerChannel, 
                _helper.Config.TimeoutS, _helper.Config.ReadbackFillMode, 
                data, out int samplesRead);
            
            if (!DAQmx.Success(result)) {
            
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("Sample");

            for (int ch = 0; ch < _helper.Config.NumberOfPhysicalChannels; ch++) {
                sb.Append($"\tChannel {ch}");
            }

            sb.Append("\n");

            for (int i = 0; i < samplesRead; i++) {
                sb.Append($"  {i + 1} ");
                for (int ch = 0; ch < _helper.Config.NumberOfPhysicalChannels; ch++) {
                    sb.Append($"\t\t{data[i * _helper.Config.NumberOfPhysicalChannels + ch]:F2}.");
                }
                sb.Append("\n");
            }

            Console.WriteLine($"Read {samplesRead} samples out of {_helper.Config.FiniteSamplesPerChannel} requested.\n{sb.ToString()}");

            result = DAQmx.IsTaskDone(handle, out bool isDone);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            Console.WriteLine($"Task is done: {isDone}.");

            result = DAQmx.StopTask(handle);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            Console.WriteLine("Task stopped.");

            result = DAQmx.DisposeTask(out handle);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            Console.WriteLine($"Task disposed. Handle: {string.Format("{0:X}", handle)}.");
            Console.WriteLine("Analog read complete");
        }

        public int EventCallback(IntPtr taskHandle, int status, ref IntPtr callbackData) {
            EndTime = DateTime.Now;
            Console.WriteLine($"Event callback called with status {status}. " +
                $"Time laps: {(EndTime - StartTime).TotalMilliseconds}ms.");
            return 0;
        }

        static DateTime StartTime;
        static DateTime EndTime;

        public void AITestFiniteSamplesSoftwareTriggerWEvent() {
            
            double[] data = new double[_helper.Config.FiniteSamplesPerChannel * 
                _helper.Config.NumberOfPhysicalChannels];
            
            Console.WriteLine("TestAILines Started.");
            Console.WriteLine("Creating a task...");

            IntPtr handle = _helper.CreateAndConfigureAioTask();
            if (handle == IntPtr.Zero) {
                Console.WriteLine("Failed to create task.");
                return;
            }

            int result = DAQmx.TaskControl(handle, TaskAction.Verify);
            if (!DAQmx.Success(result)) {
                Console.WriteLine($"Channel verification failed {DAQmx.GetErrorDescription(result)}");
                return;
            }

            Console.WriteLine("Channel verified.");

            result = DAQmx.ConfigureTiming(handle, _helper.Config.TimingSource, 
                _helper.Config.SamplingRate, ActiveEdge.Rising, 
                SamplingMode.FiniteSamples, _helper.Config.FiniteSamplesPerChannel);
            
            
            if (!DAQmx.Success(result)) {
            
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            result = DAQmx.TaskControl(handle, TaskAction.Verify);
            
            if (!DAQmx.Success(result)) {
            
                Console.WriteLine($"Timing verification failed {DAQmx.GetErrorDescription(result)}");
                return;
            }

            Console.WriteLine("Timing verified.");

            DAQmxDoneCallbackDelegate cbhandle = new DAQmxDoneCallbackDelegate(EventCallback);
            CallbackHandle callbackHandler = CallbackService.RegisterDoneEvent(handle, cbhandle, this);

            StartTime = DateTime.Now;
            result = DAQmx.StartTask(handle);
            
            if (!DAQmx.Success(result)) {
            
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            Console.WriteLine("AI task started.");
            Console.WriteLine("Reading data using ReadAnalogF64");

            result = DAQmx.ReadAnalogLines(handle, _helper.Config.FiniteSamplesPerChannel, 
                _helper.Config.TimeoutS, _helper.Config.ReadbackFillMode, 
                data, out int samplesRead);
            
            if (!DAQmx.Success(result)) {
                
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("Sample");

            for (int ch = 0; ch < _helper.Config.NumberOfPhysicalChannels; ch++) {
                sb.Append($"\tChannel {ch}");
            }

            sb.Append("\n");

            for (int i = 0; i < samplesRead; i++) {
                
                sb.Append($"  {i + 1} ");
                
                for (int ch = 0; ch < _helper.Config.NumberOfPhysicalChannels; ch++) {
                
                    sb.Append($"\t\t{data[i * _helper.Config.NumberOfPhysicalChannels + ch]:F2}.");
                }
                
                sb.Append("\n");
            }

            Console.WriteLine($"Read {samplesRead} samples out of " +
                $"{_helper.Config.FiniteSamplesPerChannel} requested.\n{sb.ToString()}");

            result = DAQmx.IsTaskDone(handle, out bool isDone);
            
            if (!DAQmx.Success(result)) {
            
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            Console.WriteLine($"Task is done: {isDone}.");

            result = DAQmx.StopTask(handle);
            
            if (!DAQmx.Success(result)) {
            
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            Console.WriteLine("Task stopped.");

            result = DAQmx.DisposeTask(out handle);
            
            if (!DAQmx.Success(result)) {
            
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

            Console.WriteLine($"Task disposed. Handle: {string.Format("{0:X}", handle)}.");
            Console.WriteLine("Analog read complete");
        }
    }

}