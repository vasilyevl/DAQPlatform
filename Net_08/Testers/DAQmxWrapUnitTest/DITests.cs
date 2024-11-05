using Grumpy.DAQmxNetApi;
using Xunit.Abstractions;

using DAQmx = Grumpy.DAQmxNetApi.DAQmxCLIWrapper;
namespace Grumpy.DAQmxWrapUnitTest
{
    public class QAQmxDITestClass
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private string deviceName = "Dev1";//"TestDevice";
        private string diChannels = "port0/line0:3";

        public QAQmxDITestClass(ITestOutputHelper testOutputHelper) {

            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Test1DILines() {

            _testOutputHelper.WriteLine("TestDILines Started.");
            IntPtr handle = IntPtr.Zero;

            _testOutputHelper.WriteLine("Creating a task...");

            Int32 result = DAQmx.CreateTask("myTask", out handle);

            Assert.True(DAQmx.Success(result), 
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"Task created. Handle: " +
                $"{string.Format("{0:X}", handle)}.");
            
            result = DAQmx.CreateDIChannel(handle,
                $"{deviceName}/{diChannels}", "myDIChannel", 
                DIOLineGrouping.ChanForAllLines);

            Assert.True(DAQmx.Success(result), 
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"DI Channel created " +
                $"for {diChannels}.");

            result = DAQmx.StartTask(handle);

            Assert.True(DAQmx.Success(result), 
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"DI task {diChannels} started.");


            byte[] data = new byte[10];
            _testOutputHelper.WriteLine("Reading data using ReadDigitalLines");

            for (int i = 0; i < 10; i++) {

                result = DAQmx.ReadDigitalLines(handle, 2, 1.0,
                    ReadbacklFillMode.ByChannel,
                    data, (uint)data.Length, out int samplesRead, 
                    out int bytesPerSample);

                Assert.True(DAQmx.Success(result), 
                    DAQmx.GetErrorDescription(result));

                var str = data.Select((x) => x.ToString("X2")).ToArray();

                _testOutputHelper.WriteLine($"Read {samplesRead} samples. " +
                    $"{bytesPerSample} bytes per sample." +
                    $"\n Data: {string.Join(",", str)}.");
            }

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

            _testOutputHelper.WriteLine($"Task disposed. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            _testOutputHelper.WriteLine(" ReadDigitalLines complete");
        }

        [Fact]
        public void Test2DIReadU32() {

            _testOutputHelper.WriteLine("TestDIRead32 Started.");
            IntPtr handle = IntPtr.Zero;

            _testOutputHelper.WriteLine("Creating a task...");

            Int32 result = DAQmx.CreateTask("myTask", out handle);

            Assert.True(DAQmx.Success(result), 
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"Task created. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            result = DAQmx.CreateDIChannel(handle, 
                $"{deviceName}/{diChannels}", 
                "myDIChannel", DIOLineGrouping.ChanForAllLines);

            Assert.True(DAQmx.Success(result), 
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"DI Channel created " +
                $"for {diChannels}.");

            result = DAQmx.StartTask(handle);

            Assert.True(DAQmx.Success(result), 
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"DI task {diChannels} started.");

            uint[] data32 = new uint[10];
            _testOutputHelper.WriteLine("Reading data using ReadDigit32");
            for (int i = 0; i < 10; i++) {

                result = DAQmx.ReadDigitU32(handle, 2, 1.0,
                    ReadbacklFillMode.ByChannel,
                    data32, (uint)data32.Length, out int samplesRead);

                Assert.True(DAQmx.Success(result),
                    DAQmx.GetErrorDescription(result));

                var str = data32.Select((x) => x.ToString("X8")).ToArray();

                _testOutputHelper.WriteLine($"Read {samplesRead} " +
                    $"samples.\n Data: {string.Join(",", str)}.");
            }

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

            _testOutputHelper.WriteLine($"Task disposed. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            _testOutputHelper.WriteLine(" ReadDigit32 complete");
        }

        [Fact]
        public void Test3DIReadU16() {

            _testOutputHelper.WriteLine("TestDIRead16 Started.");
            IntPtr handle = IntPtr.Zero;

            _testOutputHelper.WriteLine("Creating a task...");
            Int32 result = DAQmx.CreateTask("myTask", out handle);

            Assert.True(DAQmx.Success(result), 
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"Task created. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            result = DAQmx.CreateDIChannel(handle, 
                $"{deviceName}/{diChannels}", 
                "myDIChannel", DIOLineGrouping.ChanForAllLines);

            Assert.True(DAQmx.Success(result), 
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"DI Channel created for " +
                $"{diChannels}.");

            result = DAQmx.StartTask(handle);

            Assert.True(DAQmx.Success(result), 
                DAQmx.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"DI task {diChannels} started.");

            _testOutputHelper.WriteLine(" ReadDigitalLines complete");

            UInt32[] data = new UInt32[10];

            _testOutputHelper.WriteLine("Reading data using ReadDigit16");

            for (int i = 0; i < 10; i++) {

                result = DAQmx.ReadDigitU32(handle, 2, 1.0,
                    ReadbacklFillMode.ByChannel,
                    data, (uint)data.Length, out int samplesRead);

                Assert.True(DAQmx.Success(result),
                    DAQmx.GetErrorDescription(result));

                var str = data.Select((x) => x.ToString("X4")).ToArray();

                _testOutputHelper.WriteLine($"Read {samplesRead} " +
                    $"samples.\n Data: {string.Join(",", str)}.");
            }
            
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

            _testOutputHelper.WriteLine($"Task disposed. Handle: " +
                $"{string.Format("{0:X}", handle)}.");

            _testOutputHelper.WriteLine(" ReadDigit16 complete");
        }
    }
}