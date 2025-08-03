using DAQmxTester;
using Grumpy.DAQmxNetApi;
using System;
using System.Text;
using DAQmx = Grumpy.DAQmxNetApi.DAQmxCLIWrapper;

namespace Grumpy.DAQmxTester
{
    public class DAQmxDOTestClass: DAQmxTestBase
    {
        public DAQmxDOTestClass(DAQmxTestHelperConfig? config = null) : base(config) {
            Console.WriteLine("DAQmxDOTestClass initialized.");
        }
        public void DoTestLines() {
            Console.WriteLine("DoTestLines Started.");
            IntPtr handle = IntPtr.Zero;

            Console.WriteLine("Creating a task...");

            Int32 result = DAQmx.CreateTask(_helper.Config.DoTaskName, out handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task created. Handle: {string.Format("{0:X}", handle)}.");

            result = DAQmx.CreateDOChannel(handle,
                $"{_helper.Config.DeviceName}/{_helper.Config.DoChannels}",
                _helper.Config.DoChannelNameToAssign,
                DIOLineGrouping.ChanForAllLines);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"DO Channel created for {_helper.Config.DoChannels}.");

            result = DAQmx.StartTask(handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"DO task {_helper.Config.DoChannels} started.");

            byte[] data = new byte[_helper.Config.NumberOfDoChannels *
                _helper.Config.NumberOfDoSamples];

            Console.WriteLine("Writing data using WriteDigitalLines");

            for (int i = 0; i < 10; i++) {
                
                // Fill data with some pattern
                for (int j = 0; j < _helper.Config.NumberOfDoChannels * _helper.Config.NumberOfDoSamples; j++) {
                    data[j] = (byte)(i % 2 == 0 ? 0x01 : 0x00);
                }

                result = DAQmx.WriteDigitalLines(handle,
                    _helper.Config.NumberOfDoSamples, 
                    true, 
                    1.0,
                    _helper.Config.WriteFillMode,
                    data, 
                    out int samplesWritten);

                DAQmxTestHelper.Assert(DAQmx.Success(result),
                    DAQmx.GetErrorDescription(result));

                var str = data.Select((x) => x.ToString("X2")).ToArray();

                Console.WriteLine($"Wrote {samplesWritten} samples. Data: {string.Join(",", str)}.");
            }

            result = DAQmx.IsTaskDone(handle, out bool isDone);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task is done: {isDone}.");

            result = DAQmx.StopTask(handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task stopped.");

            result = DAQmx.DisposeTask(out handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task disposed. Handle: {string.Format("{0:X}", handle)}.");

            Console.WriteLine("WriteDigitalLines complete");
        }

        public void DoTestWriteU32() {
            Console.WriteLine("DoTestWrite32 Started.");
            IntPtr handle = IntPtr.Zero;

            Console.WriteLine("Creating a task...");

            Int32 result = DAQmx.CreateTask(_helper.Config.DoTaskName, out handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task created. Handle: {string.Format("{0:X}", handle)}.");

            result = DAQmx.CreateDOChannel(handle,
                $"{_helper.Config.DeviceName}/{_helper.Config.DoChannels}",
                _helper.Config.DoChannelNameToAssign,
                DIOLineGrouping.ChanForAllLines);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"DO Channel created for {_helper.Config.DoChannels}.");

            result = DAQmx.StartTask(handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"DO task {_helper.Config.DoChannels} started.");

            uint data32;
            Console.WriteLine("Writing data using WriteDigitU32");

            for (int i = 0; i < 10; i++) {
                // Fill data with some pattern

                data32 = (uint)(i % 2 == 0 ? 0xFFFFFFFF : 0x00000000);
                

                result = DAQmx.WriteDigitalScalarU32(handle, true, 1.0,
                    data32);

                DAQmxTestHelper.Assert(DAQmx.Success(result),
                    DAQmx.GetErrorDescription(result));

                var str = data32.ToString("X8");

                Console.WriteLine($"Wrote data samples.\n Data: {str}.");
            }

            result = DAQmx.IsTaskDone(handle, out bool isDone);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task is done: {isDone}.");

            result = DAQmx.StopTask(handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task stopped.");

            result = DAQmx.DisposeTask(out handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task disposed. Handle: {string.Format("{0:X}", handle)}.");

            Console.WriteLine("WriteDigitU32 complete");
        }

        public void DoTestWriteScalar() {
            Console.WriteLine("DoTestWriteScalar Started.");
            IntPtr handle = IntPtr.Zero;

            Console.WriteLine("Creating a task...");
            Int32 result = DAQmx.CreateTask(_helper.Config.DoTaskName, out handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task created. Handle: {string.Format("{0:X}", handle)}.");

            result = DAQmx.CreateDOChannel(handle,
                $"{_helper.Config.DeviceName}/{_helper.Config.DoChannels}",
                _helper.Config.DoChannelNameToAssign,
                DIOLineGrouping.ChanForAllLines);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"DO Channel created for {_helper.Config.DoChannels}.");

            result = DAQmx.StartTask(handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"DO task {_helper.Config.DoChannels} started.");

            uint data ;
            Console.WriteLine("Writing data using WriteDigitU16");

            for (int i = 0; i < 10; i++) {
                // Fill data with some pattern
        
                    data = (uint)(i % 2 == 0 ? 0xFFFF : 0x0000);
                

                result = DAQmx.WriteDigitalScalarU32(handle, true, 1.0,
       
                    data);

                DAQmxTestHelper.Assert(DAQmx.Success(result),
                    DAQmx.GetErrorDescription(result));

                var str = data.ToString("X8");

                Console.WriteLine($"Wrote scalar.\n Data: { str}.");
            }

            result = DAQmx.IsTaskDone(handle, out bool isDone);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task is done: {isDone}.");

            result = DAQmx.StopTask(handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task stopped.");

            result = DAQmx.DisposeTask(out handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task disposed. Handle: {string.Format("{0:X}", handle)}.");

            Console.WriteLine("WriteDigitU16 complete");
        }
    }
}
