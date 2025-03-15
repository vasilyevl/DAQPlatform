using System;
using System.Text;
using Grumpy.DAQmxNetApi;
using DAQmx = Grumpy.DAQmxNetApi.DAQmxCLIWrapper;

namespace Grumpy.DAQmxTester
{
    public class DAQmxAITestClass
    {
        public void Test1AISingleSamplesSoftwareTrigger() {
            Console.WriteLine("TestAILines Started.");

            double[] data = new double[DAQmxTestHelper.SamplesPerChannel * DAQmxTestHelper.NumberOfPhysicalChannels];
            IntPtr handle = DAQmxTestHelper.CreateAndConfigureAioTask();
            int result;

            for (int rn = 0; rn < DAQmxTestHelper.Runs; rn++) {
                Console.WriteLine($"\n\nRun {rn + 1} out of {DAQmxTestHelper.Runs}.");

                result = DAQmx.StartTask(handle);
                if (!DAQmx.Success(result)) {
                    Console.WriteLine(DAQmx.GetErrorDescription(result));
                    return;
                }

                Console.WriteLine("AI task started.\n");

                for (int rd = 0; rd < DAQmxTestHelper.ReadsPerRun; rd++) {
                    Console.WriteLine($"Read {rd + 1} out of {DAQmxTestHelper.ReadsPerRun}.");
                    Console.WriteLine("Reading data using ReadAnalogF64");

                    result = DAQmx.ReadAnalogF64(handle, DAQmxTestHelper.SamplesPerChannel, DAQmxTestHelper.TimeoutS, DAQmxTestHelper.ReadbackFillMode, data, out int samplesRead);
                    if (!DAQmx.Success(result)) {
                        Console.WriteLine(DAQmx.GetErrorDescription(result));
                        return;
                    }

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

                    Console.WriteLine($"Read {samplesRead} samples out of {DAQmxTestHelper.SamplesPerChannel} requested.\n{sb.ToString()}");
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

        public void Test2AIFiniteSamplesSoftwareTrigger() {
            double[] data = new double[DAQmxTestHelper.FiniteSamplesPerChannel * DAQmxTestHelper.NumberOfPhysicalChannels];
            Console.WriteLine("TestAILines Started.");

            IntPtr handle = DAQmxTestHelper.CreateAndConfigureAioTask();
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

            result = DAQmx.ConfigureTiming(handle, DAQmxTestHelper.TimingSource, DAQmxTestHelper.SamplingRate, ActiveEdge.Rising, SamplingMode.FiniteSamples, DAQmxTestHelper.FiniteSamplesPerChannel);
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

            result = DAQmx.ReadAnalogF64(handle, DAQmxTestHelper.FiniteSamplesPerChannel, DAQmxTestHelper.TimeoutS, DAQmxTestHelper.ReadbackFillMode, data, out int samplesRead);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

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

            Console.WriteLine($"Read {samplesRead} samples out of {DAQmxTestHelper.FiniteSamplesPerChannel} requested.\n{sb.ToString()}");

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
            Console.WriteLine($"Event callback called with status {status}. Time laps: {(EndTime - StartTime).TotalMilliseconds}ms.");
            return 0;
        }

        static DateTime StartTime;
        static DateTime EndTime;

        public void Test3AIFiniteSamplesSoftwareTriggerWEvent() {
            double[] data = new double[DAQmxTestHelper.FiniteSamplesPerChannel * DAQmxTestHelper.NumberOfPhysicalChannels];
            Console.WriteLine("TestAILines Started.");
            Console.WriteLine("Creating a task...");

            IntPtr handle = DAQmxTestHelper.CreateAndConfigureAioTask();
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

            result = DAQmx.ConfigureTiming(handle, DAQmxTestHelper.TimingSource, DAQmxTestHelper.SamplingRate, ActiveEdge.Rising, SamplingMode.FiniteSamples, DAQmxTestHelper.FiniteSamplesPerChannel);
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

            result = DAQmx.ReadAnalogF64(handle, DAQmxTestHelper.FiniteSamplesPerChannel, DAQmxTestHelper.TimeoutS, DAQmxTestHelper.ReadbackFillMode, data, out int samplesRead);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }

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

            Console.WriteLine($"Read {samplesRead} samples out of {DAQmxTestHelper.FiniteSamplesPerChannel} requested.\n{sb.ToString()}");

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

    public class DAQmxAOTestClass
    {


        public void Test1AOSingleSample() {
            Console.WriteLine("TestAOLines Started.");

            IntPtr handle = DAQmxTestHelper.CreateAndConfigureAioTask(
                channels: DAQmxTestHelper.AoChannels,
                taskName: DAQmxTestHelper.AoTaskName);
            int result;

            result = DAQmx.StartTask(handle);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }
            Console.WriteLine("AO task started.");

            double[] data = new double[DAQmxTestHelper.SamplesPerChannel * DAQmxTestHelper.NumberOfPhysicalChannels];
            for (int i = 0; i < data.Length; i++) {
                data[i] = i * 0.1; // Example data
            }

            result = DAQmx.WriteAnalogF64(handle, DAQmxTestHelper.SamplesPerChannel, false, DAQmxTestHelper.TimeoutS, DAQmxTestHelper.WriteFillMode, data, out int samplesWritten);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }
            Console.WriteLine($"Written {samplesWritten} samples.");

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
        }

        public void Test2AOFiniteSamples() {
            Console.WriteLine("TestAOLines Started.");

            IntPtr handle = DAQmxTestHelper.CreateAndConfigureAioTask(
                channels: DAQmxTestHelper.AoChannels,
                taskName: DAQmxTestHelper.AoTaskName);
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

            result = DAQmx.ConfigureTiming(handle, DAQmxTestHelper.TimingSource, DAQmxTestHelper.SamplingRate, ActiveEdge.Rising, SamplingMode.FiniteSamples, DAQmxTestHelper.FiniteSamplesPerChannel);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }
            Console.WriteLine("AO timing configured.");

            result = DAQmx.TaskControl(handle, TaskAction.Verify);
            if (!DAQmx.Success(result)) {
                Console.WriteLine($"Timing verification failed {DAQmx.GetErrorDescription(result)}");
                return;
            }
            Console.WriteLine("Timing verified.");

            double[] data = new double[DAQmxTestHelper.FiniteSamplesPerChannel * DAQmxTestHelper.NumberOfPhysicalChannels];

            double inc = 10.0 / data.Length;

            for (int i = 0; i < data.Length; i++) {
                data[i] = i * inc; // Example data
            }

            result = DAQmx.StartTask(handle);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }
            Console.WriteLine("AO task started.");

            result = DAQmx.WriteAnalogF64(handle, DAQmxTestHelper.FiniteSamplesPerChannel, false, DAQmxTestHelper.TimeoutS, DAQmxTestHelper.WriteFillMode, data, out int samplesWritten);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }
            Console.WriteLine($"Written {samplesWritten} samples.");

            result = DAQmx.WaitUntilTaskDone(handle, 10);
            if (!DAQmx.Success(result)) {
                Console.WriteLine(DAQmx.GetErrorDescription(result));
                return;
            }
            Console.WriteLine("Task complete.");

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
        }
    }

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