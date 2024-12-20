using Grumpy.ClickPLCDriver;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;
using System.Reflection.Metadata;
using Xunit.Abstractions;

namespace ClickPlcUnitTest
{
    public class UnitTest1
    {
        static readonly string config = 
            "{\"Interface\":{" +
                    "\"ActiveInterface\":\"Serial\"," +
                    "\"SerialPort\":{" +
                        "\"Name\": \"COM5\"," +
                        "\"BaudRate\": 38400," +
                        "\"Bits\": 8," +
                        "\"Parity\": \"Odd\"," +
                        "\"HandShake\": \"None\"," +
                        "\"StopBits\": \"One\"," +
                        "\"ReadTimeoutMs\": 200," +
                        "\"WriteTimeoutMs\": 200," +
                        "\"MinTimeBetweenTransactionsMs\": 100," +
                        "\"DelayBetweenTransactions\": 100}," +
                   "\"Network\":{\"Name\": \"ClickPLC\"," +
                                "\"IpAddress\": \"192.168.1.22\"," +
                                "\"Port\": 502," +
                                "\"Timeout\": 15000}}}";

        private static ClickPlcDriver _handler = null!;
        private readonly ITestOutputHelper _testOutputHelper;

        public UnitTest1(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
        }



        [Fact]
        public void Test1() {

            _testOutputHelper.WriteLine("Test1 started.");

             bool r = ClickPlcDriver.CreateHandler(config, out _handler!);

            Assert.True(r, "Failed to create Click PLC handler.");

            _testOutputHelper.WriteLine("Handler created.");

            r = _handler.Open();

            Assert.True(r, "Failed to open connection to Click PLC.");

            _testOutputHelper.WriteLine("Connection opened.");

            r = _handler.Close();

            Assert.True(r, "Failed to close connection to Click PLC.");

            _testOutputHelper.WriteLine("Connection closed.");

            _testOutputHelper.WriteLine("Test complete.");
         
        }

        [Fact]
        public void Test2() {

            string FloatRegister = "DF11";
            float testValue = 4095;
            int cycles = 11;
            float increment = (cycles <= 1) ? testValue : testValue / (cycles - 1);

            _testOutputHelper.WriteLine("Test2 started.");

            bool r = ClickPlcDriver.CreateHandler(config, out _handler!);

            Assert.True(r, "Failed to create Click PLC handler.");

            _testOutputHelper.WriteLine("Handler created.");

            r = _handler.Open();

            Assert.True(r, "Failed to open connection to Click PLC.");

            _testOutputHelper.WriteLine("Connection opened.");
            float readValue = 0.0f;

            for ( int i = 0; i < cycles; i++) {
                float writeValue =  ( cycles > 1) ?  increment * i : increment;
                _testOutputHelper.WriteLine(
                    $"{DateTime.Now.ToString("yy/MM/dd HH:mm:ss:fff")} " +
                    $"Writing float value {writeValue} to Click PLC " +
                    $"register {FloatRegister}.");

                r = _handler.WriteFloat32Register(FloatRegister, writeValue);

                Assert.True(r, "Failed to write float to Click PLC.");

                _testOutputHelper.WriteLine(
                    $"{DateTime.Now.ToString("yy/MM/dd HH:mm:ss:fff")} " +
                    $"Float value {writeValue} written to Click PLC " +
                    $"register {FloatRegister}.");

                r = _handler.ReadFloat32Register(FloatRegister, out readValue);

                Assert.True(r, "Failed to read float from Click PLC.");

                Assert.True(Math.Abs(writeValue - readValue) < .0001f, 
                    $"Failed to read the same value back. " +
                    $"Value received {readValue}");

                _testOutputHelper.WriteLine(
                    $"{DateTime.Now.ToString("yy/MM/dd HH:mm:ss:fff")} " +
                    $"Float value {readValue} read from Click PLC " +
                    $"register {FloatRegister}.");

                Thread.Sleep(500);
            }       

            r = _handler.Close();

            Assert.True(r, "Failed to close connection to Click PLC.");

            _testOutputHelper.WriteLine("Connection closed.");

            _testOutputHelper.WriteLine("Test complete.");
        }


        [Fact]
        public void Test3() {
            string FloatRegisterAD1 = "DF1";
            string FloatRegisterAD2 = "DF2";
            string FloatRegisterDA1 = "DF3";
            string FloatRegisterDA2 = "DF4";

            float testValueMax = 4095;
            float testValueMin = 0;

            int steps = 11;
            float increment = (steps <= 1) ? 
                (testValueMax - testValueMin) 
                : (testValueMax - testValueMin)/ (steps - 1);

            _testOutputHelper.WriteLine("Test3 (AIO) started.");

            bool r = ClickPlcDriver.CreateHandler(config, out _handler!);

            Assert.True(r, "Failed to create Click PLC handler.");

            _testOutputHelper.WriteLine("Handler created.");

            r = _handler.Open();

            Assert.True(r, "Failed to open connection to Click PLC.");

            _testOutputHelper.WriteLine("Connection opened.");
            float readValue = 0.0f;

            for ( int i = 0; i < steps; i++) {

                float da1WriteValue = testValueMin + (( steps > 1) ?  
                                            increment * i : increment);
                float da2WriteValue = testValueMax - (( steps > 1) ?  
                                            increment * i : increment);

                _testOutputHelper.WriteLine(
                    $"{DateTime.Now.ToString("yy/MM/dd HH:mm:ss:fff")} " +
                    $"Writing float value {da1WriteValue} to {FloatRegisterDA1} and " +
                    $"{da2WriteValue} to {FloatRegisterDA2}.");

                r = _handler.WriteFloat32Register(FloatRegisterDA1, da1WriteValue);

                Assert.True(r, $"Failed to write float to " +
                    $"Click PLC register {FloatRegisterDA1}.");

                r = _handler.WriteFloat32Register(FloatRegisterDA2, da2WriteValue);

                Assert.True(r, $"Failed to write float to " +
                    $"Click PLC register {FloatRegisterDA1}");

                _testOutputHelper.WriteLine(
                    $"{DateTime.Now.ToString("yy/MM/dd HH:mm:ss:fff")} " +
                    $"DAs updated.");

                Thread.Sleep(100);
                r = _handler.ReadFloat32Register(FloatRegisterAD1, out readValue);

                Assert.True(r, $"Failed to read float from Click " +
                    $"PLC register {FloatRegisterAD1}.");

                _testOutputHelper.WriteLine(
                    $"{DateTime.Now.ToString("yy/MM/dd HH:mm:ss:fff")} " +
                    $"Float value {readValue} read from Click PLC " +
                    $"register {FloatRegisterAD1}.");
                r = _handler.ReadFloat32Register(FloatRegisterAD2, out readValue);

                Assert.True(r, $"Failed to read float from Click " +
                    $"PLC register {FloatRegisterAD2}.");

                _testOutputHelper.WriteLine(
                    $"{DateTime.Now.ToString("yy/MM/dd HH:mm:ss:fff")}" +
                    $" Float value {readValue} read from Click PLC " +
                    $"register {FloatRegisterAD2}.");
              
            }       

            r = _handler.Close();

            Assert.True(r, "Failed to close connection to Click PLC.");

            _testOutputHelper.WriteLine("Connection closed.");

            _testOutputHelper.WriteLine("Test complete.");

        }


        [Fact]
        public void Test4() {

            int s = int.MinValue+1;
            _testOutputHelper.WriteLine($"Short value {Convert.ToString(s,2)}.");
            uint us = (uint) s;
            _testOutputHelper.WriteLine($"UShort value {Convert.ToString(us, 2)}.");
            int s2 = (int) us;
            _testOutputHelper.WriteLine($"Short value {Convert.ToString(s2, 2)}.");
            Assert.True(s == s2, "Failed to convert short to ushort and back.");
        }
        
    }
}