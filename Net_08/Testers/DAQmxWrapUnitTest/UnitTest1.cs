using Grumpy.DAQmxCLIWrap;
using Xunit.Abstractions;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Grumpy.DAQmxWrapUnitTest
{
    public class QAQmxTestClass
    {

        private readonly ITestOutputHelper _testOutputHelper;

        public QAQmxTestClass(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
        }


        [Fact]
        public void TestDI() {

            string diChannels = "Dev1/port0/line0:3";
            _testOutputHelper.WriteLine("TestDI Started.");
            long handle = 0;

            _testOutputHelper.WriteLine("Creating a task...");
            Int32 result = DAQmxCLIWrapper.CreateTask("myTask", out handle);
            Assert.True(DAQmxCLIWrapper.Success(result), DAQmxCLIWrapper.GetErrorDescription(result));

            _testOutputHelper.WriteLine($"Task created. Handle: {string.Format("{0:X}", handle)}.");
            
            result = DAQmxCLIWrapper.CreateDIChannel(handle, diChannels, "myDIChannel", 
                ChannelLineGrouping.ChanForAllLines);
            Assert.True(DAQmxCLIWrapper.Success(result), DAQmxCLIWrapper.GetErrorDescription(result));
            _testOutputHelper.WriteLine($"DI Channel created for {diChannels}.");

            result = DAQmxCLIWrapper.StartTask(handle);
            Assert.True(DAQmxCLIWrapper.Success(result), DAQmxCLIWrapper.GetErrorDescription(result));
            _testOutputHelper.WriteLine($"DI task {diChannels} started.");


            byte[] data = new byte[10];
            _testOutputHelper.WriteLine("Reading data using ReadDigitalLines");
            for (int i = 0; i < 10; i++) {

                result = DAQmxCLIWrapper.ReadDigitalLines(handle, 2, 1.0,
                    ChannelInterleaveMode.Interleaved,
                    data, (uint)data.Length, out int samplesRead, out int bytesPerSample);

                Assert.True(DAQmxCLIWrapper.Success(result), 
                    DAQmxCLIWrapper.GetErrorDescription(result));
                var str = data.Select((x) => x.ToString("X2")).ToArray();
                _testOutputHelper.WriteLine($"Read {samplesRead} samples. " +
                    $"{bytesPerSample} bytes per sample.\n Data: {string.Join(",", str)}.");
            }
            _testOutputHelper.WriteLine(" ReadDigitalLines complete");

            uint[] data32 = new uint[10];
            _testOutputHelper.WriteLine("Reading data using ReadDigit32");
            for (int i = 0; i < 10; i++) {

                result = DAQmxCLIWrapper.ReadDigitU32(handle, 2, 1.0,
                    ChannelInterleaveMode.Interleaved,
                    data32, (uint)data32.Length, out int samplesRead);

                Assert.True(DAQmxCLIWrapper.Success(result), 
                    DAQmxCLIWrapper.GetErrorDescription(result));
                var str = data32.Select((x) => x.ToString("X8")).ToArray();
                _testOutputHelper.WriteLine($"Read {samplesRead} " +
                    $"samples.\n Data: {string.Join(",", str)}.");
            }
            _testOutputHelper.WriteLine(" ReadDigit32 complete");

            UInt16[] data16 = new UInt16[10];
            _testOutputHelper.WriteLine("Reading data using ReadDigit16");
            for (int i = 0; i < 10; i++) {

                result = DAQmxCLIWrapper.ReadDigitU16(handle, 2, 1.0,
                    ChannelInterleaveMode.Interleaved,
                    data16, (uint)data16.Length, out int samplesRead);

                Assert.True(DAQmxCLIWrapper.Success(result),
                    DAQmxCLIWrapper.GetErrorDescription(result));
                var str = data16.Select((x) => x.ToString("X4")).ToArray();
                _testOutputHelper.WriteLine($"Read {samplesRead} " +
                    $"samples.\n Data: {string.Join(",", str)}.");
            }
            _testOutputHelper.WriteLine(" ReadDigit16 complete");

            result = DAQmxCLIWrapper.IsTaskDone(handle, out bool isDone);
            Assert.True(DAQmxCLIWrapper.Success(result), 
                DAQmxCLIWrapper.GetErrorDescription(result));
            _testOutputHelper.WriteLine($"Task is done: {isDone}.");

            result = DAQmxCLIWrapper.StopTask(handle);
            Assert.True(DAQmxCLIWrapper.Success(result), DAQmxCLIWrapper.GetErrorDescription(result));
            _testOutputHelper.WriteLine($"Task stopped.");


            result = DAQmxCLIWrapper.DisposeTask(out handle);
            Assert.True(DAQmxCLIWrapper.Success(result), DAQmxCLIWrapper.GetErrorDescription(result));
            _testOutputHelper.WriteLine($"Task disposed. Handle: {string.Format("{0:X}", handle)}.");

            _testOutputHelper.WriteLine("TestDI Complete.");



            
        }
    }
}