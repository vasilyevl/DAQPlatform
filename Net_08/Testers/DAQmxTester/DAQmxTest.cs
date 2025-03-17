using System;
using System.Text;
using Grumpy.DAQmxNetApi;
using DAQmx = Grumpy.DAQmxNetApi.DAQmxCLIWrapper;

namespace Grumpy.DAQmxTester
{
    public class DAQmxDITestClass
    {

        public void Test1DILines() {

            Console.WriteLine("TestDILines Started.");
            IntPtr handle = IntPtr.Zero;

            Console.WriteLine("Creating a task...");

            Int32 result = DAQmx.CreateTask(DAQmxTestHelper.DiTaskName, out handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task created. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            result = DAQmx.CreateDIChannel(handle,
                $"{DAQmxTestHelper.DeviceName}/{DAQmxTestHelper.DiChannels}", "myDIChannel",
                DIOLineGrouping.ChanForAllLines);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"DI Channel created " +
                $"for {DAQmxTestHelper.DiChannels}.");

            result = DAQmx.StartTask(handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"DI task {DAQmxTestHelper.DiChannels} started.");


            byte[] data = new byte[10];
            Console.WriteLine("Reading data using ReadDigitalLines");

            for (int i = 0; i < 10; i++) {

                result = DAQmx.ReadDigitalLines(handle, 2, 1.0,
                    DAQmxTestHelper.ReadbackFillMode,
                    data, (uint)data.Length, out int samplesRead,
                    out int bytesPerSample);

                DAQmxTestHelper.Assert(DAQmx.Success(result),
                    DAQmx.GetErrorDescription(result));

                var str = data.Select((x) => x.ToString("X2")).ToArray();

                Console.WriteLine($"Read {samplesRead} samples. " +
                    $"{bytesPerSample} bytes per sample." +
                    $"\n Data: {string.Join(",", str)}.");
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

            Console.WriteLine($"Task disposed. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            Console.WriteLine(" ReadDigitalLines complete");
        }


        public void Test2DIReadU32() {

            Console.WriteLine("TestDIRead32 Started.");
            IntPtr handle = IntPtr.Zero;

            Console.WriteLine("Creating a task...");

            Int32 result = DAQmx.CreateTask(DAQmxTestHelper.DiTaskName,
                out handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task created. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            result = DAQmx.CreateDIChannel(handle,
                $"{DAQmxTestHelper.DeviceName}/{DAQmxTestHelper.DiChannels}",
                DAQmxTestHelper.DiChannelNameToAssign,
                DIOLineGrouping.ChanForAllLines);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"DI Channel created " +
                $"for {DAQmxTestHelper.DiChannels}.");

            result = DAQmx.StartTask(handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"DI task " +
                $"{DAQmxTestHelper.DiChannels} started.");

            uint[] data32 = new uint[10];
            Console.WriteLine("Reading " +
                "data using ReadDigit32");

            for (int i = 0; i < 10; i++) {

                result = DAQmx.ReadDigitU32(handle, 2, 1.0,
                    DAQmxTestHelper.ReadbackFillMode,
                    data32, (uint)data32.Length, out int samplesRead);

                DAQmxTestHelper.Assert(DAQmx.Success(result),
                    DAQmx.GetErrorDescription(result));

                var str = data32.Select((x) => x.ToString("X8")).ToArray();

                Console.WriteLine($"Read {samplesRead} " +
                    $"samples.\n Data: {string.Join(",", str)}.");
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

            Console.WriteLine($"Task disposed. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            Console.WriteLine(" ReadDigit32 complete");
        }


        public void Test3DIReadU16() {

            Console.WriteLine("TestDIRead16 Started.");
            IntPtr handle = IntPtr.Zero;

            Console.WriteLine("Creating a task...");
            Int32 result = DAQmx.CreateTask(DAQmxTestHelper.DiTaskName,
                out handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"Task created. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            result = DAQmx.CreateDIChannel(handle,
                $"{DAQmxTestHelper.DeviceName}/{DAQmxTestHelper.DiChannels}",
                DAQmxTestHelper.DiChannelNameToAssign,
                DIOLineGrouping.ChanForAllLines);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"DI Channel created for " +
                $"{DAQmxTestHelper.DiChannels}.");

            result = DAQmx.StartTask(handle);

            DAQmxTestHelper.Assert(DAQmx.Success(result),
                DAQmx.GetErrorDescription(result));

            Console.WriteLine($"DI task {DAQmxTestHelper.DiChannels} started.");

            Console.WriteLine(" ReadDigitalLines complete");

            UInt32[] data = new UInt32[10];

            Console.WriteLine("Reading data using ReadDigit16");

            for (int i = 0; i < 10; i++) {

                result = DAQmx.ReadDigitU32(handle, 2, 1.0,
                    DAQmxTestHelper.ReadbackFillMode,
                    data, (uint)data.Length, out int samplesRead);

                DAQmxTestHelper.Assert(DAQmx.Success(result),
                    DAQmx.GetErrorDescription(result));

                var str = data.Select((x) => x.ToString("X4")).ToArray();

                Console.WriteLine($"Read {samplesRead} " +
                    $"samples.\n Data: {string.Join(",", str)}.");
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

            Console.WriteLine($"Task disposed. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            Console.WriteLine(" ReadDigit16 complete");
        }
    }

}