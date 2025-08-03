using System;
using System.Text;
using System.Threading.Channels;
using DAQmxTester;
using Grumpy.DAQmxNetApi;
using DAQmxWrapper = Grumpy.DAQmxNetApi.DAQmxCLIWrapper;

namespace Grumpy.DAQmxTester
{


    public class DAQmxAOTestClass:DAQmxTestBase
    {

        public DAQmxAOTestClass(DAQmxTestHelperConfig? config = null)
            : base(config ?? new DAQmxTestHelperConfig()) { }

        public void AOTestSingleSample(uint cycles = 100, uint smpls = 300) {
            
            Console.WriteLine("Test AOLines Started.");

            cycles = Math.Max(1, cycles);

            IntPtr handle = _helper.CreateAndConfigureAioTask(
                channels: _helper.Config.AoChannels,
                taskName: _helper.Config.AoTaskName);
            int result;

            result = DAQmxWrapper.StartTask(handle);
            
            if (!DAQmxWrapper.Success(result)) {
            
                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            double[][] data = new double [smpls][];

            for (int i = 0; i < smpls; i++) {

                int samples = _helper.Config.SamplesPerChannel 
                    * _helper.Config.NumberOfPhysicalChannels;
                data[i] = new double[samples];

                for (int j = 0; j < samples; j++) {
                    data[i][j] = 5.0 * (i + 1) / (smpls); // Example data
                }
            }

            Console.WriteLine("AO task started. Writing data:");

            for (int cl = 0; cl < cycles; cl++) {

                for (int i = 0; i < cycles; i++) {

                    result = DAQmxWrapper.WriteAnalogF64(handle,
                        _helper.Config.SamplesPerChannel, 
                        false,
                        _helper.Config.TimeoutS,
                        _helper.Config.WriteFillMode, 
                        data[i], 
                        out int samplesWritten);
                    
                    if (!DAQmxWrapper.Success(result)) {

                        Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                        return;
                    }

                    Console.WriteLine($"Cycle #{i + 1}. Written " +
                        $"{samplesWritten} samples.");
                }
            }

            result = DAQmxWrapper.StopTask(handle);
            
            if (!DAQmxWrapper.Success(result)) {
            
                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            Console.WriteLine("Task stopped.");

            result = DAQmxWrapper.DisposeTask(out handle);
            
            if (!DAQmxWrapper.Success(result)) {
                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            Console.WriteLine($"Task disposed. Handle: " +
                $"{string.Format("{0:X}", handle)}.");
        }


        public void AOTestFiniteSamples() {
            
            Console.WriteLine("TestAOLines Started.");

            IntPtr handle = _helper.CreateAndConfigureAioTask(
                channels: _helper.Config.AoChannels,
                taskName: _helper.Config.AoTaskName);
            
            if (handle == IntPtr.Zero) {
            
                Console.WriteLine("Failed to create task.");
                return;
            }

            int result = DAQmxWrapper.TaskControl(handle, TaskAction.Verify);
            
            if (!DAQmxWrapper.Success(result)) {
            
                Console.WriteLine($"Channel verification " +
                    $"failed {DAQmxWrapper.GetErrorDescription(result)}");
                return;
            }

            Console.WriteLine("Channel verified.");

            result = DAQmxWrapper.ConfigureTiming(handle, 
                _helper.Config.TimingSource,
                _helper.Config.SamplingRate, 
                ActiveEdge.Rising, 
                SamplingMode.FiniteSamples, 
                _helper.Config.FiniteSamplesPerChannel);
            
            if (!DAQmxWrapper.Success(result)) {
            
                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            Console.WriteLine("AO timing configured.");

            result = DAQmxWrapper.TaskControl(handle, TaskAction.Verify);
            
            if (!DAQmxWrapper.Success(result)) {
            
                Console.WriteLine($"Timing verification failed " +
                    $"{DAQmxWrapper.GetErrorDescription(result)}");
                return;
            }

            Console.WriteLine("Timing verified.");

            double[] data = new double[_helper.Config.FiniteSamplesPerChannel 
                * _helper.Config.NumberOfPhysicalChannels];

            double inc = 10.0 / data.Length;

            for (int i = 0; i < data.Length; i++) {
            
                data[i] = i * inc; // Example data
            }

            result = DAQmxWrapper.StartTask(handle);
            
            if (!DAQmxWrapper.Success(result)) {
            
                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            Console.WriteLine("AO task started.");

            result = DAQmxWrapper.WriteAnalogF64(handle, 
                _helper.Config.FiniteSamplesPerChannel, 
                false,
                _helper.Config.TimeoutS,
                _helper.Config.WriteFillMode, 
                data, 
                out int samplesWritten);
            
            if (!DAQmxWrapper.Success(result)) {
            
                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            Console.WriteLine($"Written {samplesWritten} samples.");

            result = DAQmxWrapper.WaitUntilTaskDone(handle, 10);
            
            if (!DAQmxWrapper.Success(result)) {
            
                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }
            
            Console.WriteLine("Task complete.");

            result = DAQmxWrapper.StopTask(handle);
            
            if (!DAQmxWrapper.Success(result)) {
            
                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            Console.WriteLine("Task stopped.");

            result = DAQmxWrapper.DisposeTask(out handle);
            
            if (!DAQmxWrapper.Success(result)) {
            
                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            Console.WriteLine($"Task disposed. Handle: " +
                $"{string.Format("{0:X}", handle)}.");
        }


        public void AOTestFiniteSamplesExtTrgr() {

            Console.WriteLine("TestAOLines Started.");

            IntPtr handle = _helper.CreateAndConfigureAioTask(
                channels: _helper.Config.AoChannels,
                taskName: _helper.Config.AoTaskName);

            if (handle == IntPtr.Zero) {

                Console.WriteLine("Failed to create task.");
                return;
            }

            int result = DAQmxWrapper.TaskControl(handle, TaskAction.Verify);

            if (!DAQmxWrapper.Success(result)) {

                Console.WriteLine($"Channel verification " +
                    $"failed {DAQmxWrapper.GetErrorDescription(result)}");
                return;
            }

            Console.WriteLine("Channel verified.");

            result = DAQmxWrapper.ConfigureTiming(handle,
                _helper.Config.TimingSource,
                _helper.Config.SamplingRate,
                ActiveEdge.Rising,
                SamplingMode.FiniteSamples,
                _helper.Config.FiniteSamplesPerChannel);

            if (!DAQmxWrapper.Success(result)) {

                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            Console.WriteLine("AO timing configured.");

            result = DAQmxWrapper.TaskControl(handle, TaskAction.Verify);

            if (!DAQmxWrapper.Success(result)) {

                Console.WriteLine($"Timing verification failed " +
                    $"{DAQmxWrapper.GetErrorDescription(result)}");
                return;
            }

            Console.WriteLine("Timing verified.");

            Console.WriteLine("Configuring Start Trigger.");

            result = DAQmxWrapper.ConfigureStartTrigger(handle,
                $"{_helper.Config.DeviceName}/{_helper.Config.StartTriger}",
                _helper.Config.StartTriggerEdge);

            

            if (!DAQmxWrapper.Success(result)) {

                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            Console.WriteLine("External trigger configured.");

            result = DAQmxWrapper.TaskControl(handle, TaskAction.Verify);

            if (!DAQmxWrapper.Success(result)) {

                Console.WriteLine($"External Trigger verification failed " +
                    $"{DAQmxWrapper.GetErrorDescription(result)}");
                return;
            }

            Console.WriteLine("External trigger verified.");


            double[] data = new double[_helper.Config.FiniteSamplesPerChannel
                * _helper.Config.NumberOfPhysicalChannels];

            double inc = 10.0 / data.Length;

            for (int i = 0; i < data.Length; i++) {

                data[i] = i * inc; // Example data
            }

            result = DAQmxWrapper.StartTask(handle);

            if (!DAQmxWrapper.Success(result)) {

                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            Console.WriteLine("AO task started.");

            result = DAQmxWrapper.WriteAnalogF64(handle,
                _helper.Config.FiniteSamplesPerChannel,
                false,
                _helper.Config.TimeoutS,
                _helper.Config.WriteFillMode,
                data,
                out int samplesWritten);

            if (!DAQmxWrapper.Success(result)) {

                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            Console.WriteLine($"Task is ready. Please generate triggr at " +
                $"{_helper.Config.DeviceName}/{_helper.Config.StartTriger}. \n" +
                $"Timeout {_helper.Config.ExternalTriggerTimeoutS}s.");


            Console.WriteLine($"Written {samplesWritten} samples.");

            result = DAQmxWrapper.WaitUntilTaskDone(handle, 
                _helper.Config.ExternalTriggerTimeoutS +
                _helper.Config.AOFinateTaskTimeout);

            if (!DAQmxWrapper.Success(result)) {

                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                DAQmxWrapper.StopTask(handle);
                DAQmxWrapper.DisposeTask(out handle);
                return;
            }

            Console.WriteLine("Task complete.");

            result = DAQmxWrapper.StopTask(handle);

            if (!DAQmxWrapper.Success(result)) {

                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            Console.WriteLine("Task stopped.");

            result = DAQmxWrapper.DisposeTask(out handle);

            if (!DAQmxWrapper.Success(result)) {

                Console.WriteLine(DAQmxWrapper.GetErrorDescription(result));
                return;
            }

            Console.WriteLine($"Task disposed. Handle: " +
                $"{string.Format("{0:X}", handle)}.");
        }
    }
}