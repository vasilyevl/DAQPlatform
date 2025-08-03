using System;
using DAQmxTester;
using Grumpy.DAQmxNetApi;
using DAQmx = Grumpy.DAQmxNetApi.DAQmxCLIWrapper;

namespace Grumpy.DAQmxTester
{
    public class CounterTest : DAQmxTestBase
    {

        public CounterTest(DAQmxTestHelperConfig? config = null) : base(config) {
            Console.WriteLine("CounterTest initialized.");
        }

        private DAQmxTestHelper _deviceHelper = new DAQmxTestHelper();

        public void TestCreateCOPulseChanTime() {
            Console.WriteLine("TestCreateCOPulseChanTime Started.");
            IntPtr handle = IntPtr.Zero;

            Console.WriteLine("Creating a task...");

            int result = DAQmx.CreateTask("CounterTask", out handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task created. Handle: {string.Format("{0:X}", handle)}.");

            result = DAQmx.CreateCOPulseChanTime(taskHandle:handle,
                counter:$"{_deviceHelper.Config.DeviceName}/{_deviceHelper.Config.CounterChannel}", 
                nameToAssignToChannel: _deviceHelper.Config.CounterAssignedName,
                units:TimeUnits.Seconds, 
                idleState:DioState.Low,
                initialDelay: 0.0, 
                lowTime: 1.0, 
                highTime:0.5);


            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine("Counter Output Pulse Channel created.");
            
           /* result = DAQmx.ConfigureTiming(taskHandle: handle,
                source: $"/{DAQmxTestHelper.DeviceName}/PFI01",
                rate: 100000.0,
                activeEdge: ActiveEdge.Rising,
                sampleMode: SamplingMode.FiniteSamples,
                sampsPerChan: 1);
           */

            result = DAQmx.StartTask(handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine("Counter task started.");
            result = DAQmx.WaitUntilTaskDone(handle, 10.0);
            DAQmxTestHelper.Assert(DAQmx.Success(result),
                                    DAQmx.GetErrorDescription(result));
            // Add any additional test logic here

            result = DAQmx.StopTask(handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine("Task stopped.");

            result = DAQmx.DisposeTask(out handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task disposed. Handle: {string.Format("{0:X}", handle)}.");

            Console.WriteLine("TestCreateCOPulseChanTime complete");
        }
    }
}