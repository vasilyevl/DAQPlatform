using LV.ClickPLCHandler;
using LV.HWControl.Common;
using LV.HWControl.Common.Handlers;

using Newtonsoft.Json;

using System;
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
        static TestType _testType = TestType.IOArray;
        static int cntr = 0;
        static string _io  = "C1";
        static string _timer = "T1";
        static string _setPoint = "DS10";
        static string _controlRelay = "C16";
        static int timeSetTime = 1234;
        static int exitCode = 0;
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

            Console.WriteLine("Handler opened.");

            Console.WriteLine("Starting simple single Descrete IO test test.");
            if((exitCode = DoSingleIO(_io)) != 0) {
                Console.WriteLine($"Simple single Descrete IO test failed with exit code {exitCode} .");
                goto Exit;
            }

            Console.WriteLine("Simple single Descrete IO test complete.");


            Console.WriteLine("Starting Descrete IO array test.");


            SwitchCtrl[] control = Enumerable.Repeat(SwitchCtrl.On, 8).ToArray();

            Console.WriteLine($"Setting all IOs to {SwitchCtrl.On}.");

            if( (exitCode = DoDiscreteIOs(_io, control)) != 0) {
                Console.WriteLine($"Descrete IO array test failed with exit code {exitCode} .");
                goto Exit;
            }
            Thread.Sleep(2000);
            control = Enumerable.Repeat(SwitchCtrl.Off, 8).ToArray();

            Console.WriteLine($"Setting all IOs to {SwitchCtrl.Off}.");
            
            if ((exitCode = DoDiscreteIOs(_io, control)) != 0) {
                Console.WriteLine($"Descrete IO array test failed with exit code {exitCode} .");
                goto Exit;
            }

            Console.WriteLine("Descrete IO array test complete.");
            Thread.Sleep(2000);

            Console.WriteLine("Starting simple timer test.");

            if( (exitCode = DoSimpleTimerTest(_timer, _setPoint, _controlRelay, timeSetTime)) != 0) {
                Console.WriteLine($"Simple timer test failed with exit code {exitCode} .");
                goto Exit;
            }
            Console.WriteLine("Simple timer test complete.");

                
            /*
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
            */
            Exit:
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

       
        private static int DoDiscreteIOs( string io, SwitchCtrl[] controls)
        {

            if (!_handler.ReadDiscreteIOs(io, controls.Length, out SwitchSt[] status)) {
                Console.WriteLine($"Failed to read {io}x{controls.Length} relays status.");
                return -1;
            }
            
            if ( status.Where( (o) => o == SwitchSt.Unknown).Count() > 0) {

                Console.WriteLine($"Failed to read {io}x{controls.Length} relays status. " +
                    $"Some IOs have unknown status: {string.Join(" ," ,status)}");
                return -2;
            }

            Console.WriteLine($"{io} relays status is {string.Join(" ,", status)}.");

            if (!_handler.WriteDiscreteControls(io, controls)) {
                Console.WriteLine($"Failed to write {io}x{controls.Length} relays status.");
                return -3;
            }   

            Console.WriteLine($"{io} relays set to {string.Join(" ,", controls)}.");

            if (!_handler.ReadDiscreteIOs(io, controls.Length, out status)) {
                Console.WriteLine($"Failed to read {io}x{controls.Length} relays status.");
                return -4 ;
            }

            if (status.Where((o) => o == SwitchSt.Unknown).Count() > 0) {

                Console.WriteLine($"Failed to read {io}x{controls.Length} relays status. " +
                    $"Some IOs have unknown status: {string.Join(" ,", status)}");
                return -5;
            }

            Console.WriteLine($"{io} relays status is {string.Join(" ,", status)}.");

            return 0;
        }

        private static int DoSingleIO( string io, int repeats = 4)
        {
            SwitchSt status;

            for (int i = 0; i < repeats; i++) {
                Thread.Sleep(100);
                SwitchCtrl ctrl = (i % 2) == 0 ? SwitchCtrl.On : SwitchCtrl.Off;

                if (!_handler.WriteDiscreteControl(io, ctrl)) {
                    return -1;
                }

                Console.WriteLine($"{io} relay set to {ctrl}.");
                Thread.Sleep(100);

                if (!_handler.ReadDiscreteIO(io, out status)) {
                    return -2;
                }

                Console.WriteLine($"{io} relay status is {status}.");
            }                   
            return 0;
        }


        private static int DoSimpleTimerTest( string timer, string setRegister, string controlRelay,  int timerTimeMs) {

            string timerCurrentValueRegister = timer.Replace("T", "TD");

            if (_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.Off)) {
                Console.WriteLine($"Control relay {controlRelay} switched off.");
            }
            else {
                Console.WriteLine($"Failed to switch control relay {controlRelay} to off.");
                return -1;
            }

            if (_handler.ReadRegister(setRegister, out ushort timerValue)) {
                Console.WriteLine($"Current timer set value {timerValue}.");
            }
            else {
                Console.WriteLine($"Failed to read current set value from {setRegister}.");
                return -2;
            }

            Thread.Sleep(1000);

            if (_handler.WriteRegister(setRegister, (ushort)timerTimeMs)) {
                Console.WriteLine($"Set timer value to {timerTimeMs}.");
            }
            else {
                Console.WriteLine($"Failed to set register {setRegister} to {timerTimeMs}.");
                return -3;
            }

            if (_handler.ReadRegister(setRegister, out timerValue)) {
                Console.WriteLine($"Current timer set value: {timerValue}.");
            }
            else {
                Console.WriteLine($"Failed to read current set value from {setRegister}.");
                return -4;
            }

            if (_handler.ReadRegister(timerCurrentValueRegister, out timerValue)) {
                Console.WriteLine($"Current timer counter value: {timerValue}.");
            }
            else {
                Console.WriteLine($"Failed to read current timer " +
                    $"counter value form {timerCurrentValueRegister}.");
                return -5;
            }


            if (_handler.ReadDiscreteIO(timer, out SwitchState switchState)) {
                Console.WriteLine($"Current timer output state: {switchState}.");
            }
            else {
                Console.WriteLine($"Failed to read current timer " +
                    $"output state value form {timer}.");
                return -6;
            }


            if (_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.On)) {
                Console.WriteLine($"Control relay {controlRelay} switched On.");
            }
            else {
                Console.WriteLine($"Failed to switch control relay {controlRelay} to off.");
                return -1;
            }

            while (true) {
               
                if(!_handler.ReadDiscreteIO(timer, out switchState)) {
                    Console.WriteLine($"Failed to read timer {timer} state.");
                    return -7;
                }

                if(!_handler.ReadRegister(timerCurrentValueRegister, out timerValue)) {
                    Console.WriteLine($"Failed to read timer current value from {timerCurrentValueRegister}.");
                    return -8;
                }

                switch ( switchState.State) {

                    case SwitchSt.On:
                        Console.WriteLine($"Timer tripped. Current countvalue: {timerValue}.");
                        Thread.Sleep(2500);
                        if (_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.Off)) {
                            Console.WriteLine($"Control relay {controlRelay} switched off.");
                       

                            if (_handler.WriteRegister(setRegister, 0)) {
                                Console.WriteLine($"Timer set point cleared.");
                                Thread.Sleep(2500);
                                return 0;
                            }
                            else {
                                Console.WriteLine($"Failed to set timer set pint register {setRegister} to 0.");
                                return -12;
                            }
                            
                        }
                        else {
                            Console.WriteLine($"Failed to switch control relay {controlRelay} to off.");
                            return -9;
                        }
                        

                    case SwitchSt.Off:
                        Console.WriteLine($"Timer is off at {timerValue} counts.");
                        break;

                    case SwitchSt.Unknown:
                        Console.WriteLine($"Error. Timer state is unknown at {timerValue} counts.");

                        if (_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.Off)) {
                            Console.WriteLine($"Control relay {controlRelay} switched off.");
                        }
                        else {
                            Console.WriteLine($"Failed to switch control relay {controlRelay} to off.");
                            return -11;
                        }
                        return -10;
                }
                Thread.Sleep(250);
            }
        }
    }
}
