using LV.ClickPLCHandler;
using LV.HWControl.Common;
using LV.HWControl.Common.Handlers;

using Newtonsoft.Json;

using System;

using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ClickTester
{

    public enum TestType
    {
        SingleIO,
        IOArray
    }
    internal class Program
    {
        static ClickPLCHandler _handler;
        static bool _consoleOutputEnabled = false;
        static int _sycles = 10;
        static TestType _testType = TestType.IOArray;
        static int cntr = 0;
        static string _io  = "C1";

        static int Main(string[] args)
        {            
            int exitCode = 0;
     
            _handler = ClickPLCHandler.CreateHandler();
            ClickHandlerConfiguration cnfg = _GenerateConfiguration();

            _handler.Init(JsonConvert.SerializeObject(cnfg));

            if (!_handler.Open()) { 
                
                exitCode = -1;
                Console.WriteLine("Failed to open handler.");
                return exitCode;
            }

            var ct = _handler.GetRelayControlRW("C101");

            bool res = ct.Set(SwitchCtrl.On);
            res = ct.Get(out SwitchState st);
            Thread.Sleep(333);
            res = ct.Set(SwitchCtrl.Off);
            res = ct.Get(out st);

            var ct1 = _handler.GetRelayControlRW("C1");
            res = ct1.Set(SwitchCtrl.On);
            res = ct1.Get(out st);
            Thread.Sleep(333);
            res = ct1.Set(SwitchCtrl.Off);
            res = ct1.Get(out st);

            Console.WriteLine("Srting IO test.");

            cntr = 0;
    
            string timerName = "T1";
            string timerPreset = "DS10";
            ushort timerTimeMs = 20000;
            string controlRelay  = "C16";


            //exitCode = DoSimpleTimerTest(timerValue, timerPreset, controlRelay, timerTimeMs);
             exitCode = DoTimerObjectTest(timerName, timerPreset, timerTimeMs, controlRelay);

            try {
                Stopwatch stp = new Stopwatch();
                stp.Start();

                for (int i = 0; i < _sycles; i++) {

                    switch (_testType) {

                        case TestType.SingleIO:
                            var cntrl = (cntr++ / 2) == 0 ? SwitchCtrl.On : SwitchCtrl.Off;
                            exitCode = DoSingleIO(_io, cntrl);
                            break;

                        case TestType.IOArray:
                            SwitchCtrl[] control = Enumerable.Repeat(SwitchCtrl.Off, 16).ToArray();
                            control[(cntr++) % control.Length] = SwitchCtrl.On;
                            exitCode = DoArrayIO(_io, control);
                            break;
                    }

                    if (exitCode != 0) { break; }
                }

                stp.Stop();
                long duration = stp.ElapsedMilliseconds;

                Console.WriteLine($"Total time {duration}ms, " +
                    $"{duration / (_sycles * 2.0)}ms per sycle.");
            }
            catch (Exception ex) {
                
                Console.WriteLine($"Test failed with exception: {ex.Message}");
                exitCode = -1;
            }

            Console.WriteLine($"Test complete with error code {exitCode}");
            Console.WriteLine("Click 'Enter' to exit.");
            Console.ReadLine();
            return exitCode;
        }

        private static ClickHandlerConfiguration _GenerateConfiguration()
        {
            var EnetConfig = new EthernetConnectionConfiguration() {
                Name = "ClickPLC",
                IpAddress = "192.168.1.35",
                Port = 502
            };

            var cnfg = new ClickHandlerConfiguration() {

                Interface = new InterfaceConfiguration() {
                    Selector = InterfaceSelector.Network,
                    SerialPort = null,
                    Network = EnetConfig
                }
            };

            return cnfg;
        }

        private static int DoArrayIO( string io, SwitchCtrl[] control)
        {
            if (!_handler.WriteDiscreteIOs(io, control)) {
                return -2;
            }

            if (!_handler.ReadDiscreteIOs(io, 16, out SwitchSt[] status)) {
                return -2;
            }

            if (_consoleOutputEnabled) {
                Console.WriteLine($"{io} relay status is {status}.");
            }

            for( int i = 0; i < status.Length; i++) {
                if ((int)control[i] != (int)status[i]) {
                    return -3;
                }
            }

            return 0;
        }

        private static int DoSingleIO( string io, SwitchCtrl sw)
        {
            if (!_handler.WriteDiscreteControl(io, sw)) {
                return -5;
            }

            if (_consoleOutputEnabled) {
                Console.WriteLine($"{io} relay set to Off.");
            }

            if (!_handler.ReadDiscreteIO(io, out SwitchSt status)) {
                return -6;
            }

            if (_consoleOutputEnabled) {
                Console.WriteLine($"{io} relay status is {status}.");
            }

            if ((int)status != (int)sw) {
                Console.WriteLine($"Failed to set \"{io}\" to {sw}");
                return -7;
            }

            return 0;
        }

        private static int DoTimerObjectTest( string  timerName, 
            string setPointRegister, int timerSetValue,  string controlRelay) 
            {
            var timerCtrl = _handler.GetTimerCtrl(timerName: timerName, setPointName: setPointRegister);
            
            timerCtrl.SetSetPoint((ushort)timerSetValue);
                 
            SwitchCtrl ctrl = SwitchCtrl.Off;

            if (_handler.WriteDiscreteControl(controlRelay, ctrl)) {
                Console.WriteLine($"{controlRelay} set to {ctrl}");
                Thread.Sleep(500);
            }
            else {
                Console.WriteLine($"Failed to set {controlRelay}");
                return -1;
            }

            ctrl = SwitchCtrl.On;
            if (_handler.WriteDiscreteControl(controlRelay, ctrl)) {
                Console.WriteLine($"{controlRelay} set to {ctrl}");
                Thread.Sleep(50);
            }
            else {
                Console.WriteLine($"Failed to set {controlRelay}");
                return -1;
            }

            while (true) {

                Thread.Sleep(250);

                var status = timerCtrl.GetState().State;
                bool result = timerCtrl.GetCounts(out ushort timer);


                Console.WriteLine($"Timer status: {status}. Counts {timer}.");

                if (status == SwitchSt.On) {
                    Console.WriteLine("Timer tripped.");
                    return 0;
                }
                else if (status == SwitchSt.Unknown) {
                    Console.WriteLine($"Failed to read timer state.");
                    return -4;
                }
            }



        }


        private static int DoSimpleTimerTest(string timerValue, string timerPreset, 
            string controlRelay,  int timerTimeMs) {

            SwitchCtrl ctrl = SwitchCtrl.Off;

            if (_handler.WriteDiscreteControl(controlRelay, ctrl)) {
                Console.WriteLine($"{controlRelay} set to {ctrl}");
                Thread.Sleep(500);
            }
            else {
                Console.WriteLine($"Failed to set {controlRelay}");
                return  -1;
            }

            bool r = _handler.WriteRegister("DS1", 0xFF);

            if (_handler.WriteRegister(timerPreset, (ushort) timerTimeMs)) {
                Console.WriteLine($"Timer preset {timerPreset} set to {timerTimeMs}ms.");
            }
            else {
                Console.WriteLine($"Failed to set timer preset {timerPreset}.");
                return -2;
            }

            if (_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.On)) {
                Console.WriteLine($"Starting trigger using {controlRelay}.");
            }
            else {
                Console.WriteLine($"Failed to start timer using {controlRelay}.");
                return  -3;
            }

            SwitchSt status = SwitchSt.Unknown;


            while (true) {
                Thread.Sleep(250);
                r = _handler.ReadDiscreteIO("T1", out status);

                Console.WriteLine($"Timer status: {status}.");

                if (status == SwitchSt.On) {
                    return 0;
                }
                else if (status == SwitchSt.Unknown) {
                    Console.WriteLine($"Failed to read timer state.");
                    return -4;
                }
            }
        }
    }
}
