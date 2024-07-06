using Grumpy.ClickPLC;
using Grumpy.HWControl.Common;
using Grumpy.HWControl.Common.Handlers;

using Newtonsoft.Json;

using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;


namespace ClickTester
{

    public enum TestType
    {
        SingleIO,
        IOArray
    }
    internal class Program
    {
        static  ClickHandler _handler = null!;
        static string _io = "C1";
        static string _timer = "T1";
        static string _setPoint = "DS10";
        static string _controlRelay = "C16";
        static int _setTimeMs = 2500;

        static int Main(string[] args) {
            int exitCode = 0;

            

            var configuration = new ClickHandlerConfiguration() {

                Interface = new InterfaceConfiguration() {
                    Selector = InterfaceSelector.Network,
                    SerialPort = null,
                    Network = new EthernetConnectionConfiguration() {
                        Name = "ClickPLC",
                        IpAddress = "192.168.1.22",
                        Port = 502
                    }
                }
            };

            string cnfg = JsonConvert.SerializeObject(configuration);

            if (!ClickHandler.CreateHandler(configuration, out _handler!)) {
                Console.WriteLine("Failed to create handler.");
                return -1;
            }
           

            if (!_handler.Open()) {

                exitCode = -1;
                Console.WriteLine("Failed to open handler.");
                return exitCode;
            }

            Console.WriteLine("Handler opened.");

            Console.WriteLine("Starting simple single discrete IO test test.");

            if ((exitCode = DoSingleIO(_io)) != 0) {
                Console.WriteLine($"Simple single discrete IO test failed" +
                    $" with exit code {exitCode} .");
                goto Exit;
            }

            Console.WriteLine("Simple single discrete IO test complete.");


            Console.WriteLine("Starting discrete IO array test.");

            if ((exitCode = DoMultipleRelaysTest("C2", 8)) != 0) {
                Console.WriteLine($"Discrete IO array test failed with exit " +
                    $"code {exitCode} .");
                goto Exit;
            }


            Console.WriteLine("Discrete IO array test complete.");
            Thread.Sleep(1000);

            exitCode = DoSimpleCounterTest("CT1", "DS11", "C10", "C11", 25);
            if (exitCode != 0) {
                Console.WriteLine($"Simple counter test failed with " +
                    $"exit code {exitCode} .");
                goto Exit;
            }
            Console.WriteLine("Simple counter test complete.");

            Console.WriteLine("Starting simple timer test.");
            exitCode = DoSimpleTimerTest(_timer, _setPoint, _controlRelay, _setTimeMs);
            if (exitCode != 0) {
                Console.WriteLine($"Simple timer test failed with exit " +
                    $"code {exitCode} .");
                goto Exit;
            }
            Console.WriteLine("Simple timer test complete.");


            Exit:
            Console.WriteLine($"Test complete with error code {exitCode}");
            Console.WriteLine("Click 'Enter' to exit.");
            Console.ReadLine();
            return exitCode;
        }


        private static int DoMultipleRelaysTest(string startRelay, int quantity) {

            SwitchCtrl[] controlArray = new SwitchCtrl[quantity];

            controlArray = controlArray.Select((x) => SwitchCtrl.On).ToArray();

            int errorCode;
            if ((errorCode = DoDiscreteIOs(startRelay, controlArray)) != 0) {
                return errorCode;
            }

            int cntr = 0;
            controlArray = controlArray.Select((x) => (cntr++ % 2 == 0 ? SwitchCtrl.Off : SwitchCtrl.On)).ToArray();
            Thread.Sleep(2000);
            if ((errorCode = DoDiscreteIOs(startRelay, controlArray)) != 0) {
                return errorCode;
            }

            cntr = 0;
            controlArray = controlArray.Select((x) => (cntr++ % 2 == 0 ? SwitchCtrl.On : SwitchCtrl.Off)).ToArray();
            Thread.Sleep(2000);
            if ((errorCode = DoDiscreteIOs(startRelay, controlArray)) != 0) {
                return errorCode;
            }
            Thread.Sleep(2000);
            controlArray = controlArray.Select((x) => SwitchCtrl.Off).ToArray();
            errorCode = DoDiscreteIOs(startRelay, controlArray);

            return errorCode;
        }


        private static int DoDiscreteIOs(string io, SwitchCtrl[] controls, 
            string startInput = "X1", int inputLen = 8, string startOutput = "Y1", int outpLen = 6) {
            
            try {
                int startRelayIdx = int.Parse(Regex.Match(io, @"\d+").Value);
            }
            catch {

                Console.WriteLine($"Unable to parse '{io}'");
                return -6;
            }

            if (!_handler.ReadDiscreteIOs(io, controls.Length, out SwitchState[] status)) {
                Console.WriteLine($"Failed to read status of " +
                    $"{controls.Length} relays starting {io}.");
                return -1;
            }

            if (status.Where((o) => o.State == SwitchSt.Unknown).Count() > 0) {

                Console.WriteLine($"Failed to read status of " +
                    $"{controls.Length} relays starting {io}.  " +
                    $"Some IOs have unknown status: " +
                    $"{string.Join(" ,", status.Select((o) => o.ToString()).ToArray())}.");
                return -2;
            }

            Console.WriteLine($"{controls.Length} relays " +
                $"starting {io} status is " +
                $"{string.Join(" ,", status.Select((o) => o.ToString()).ToArray())}.");


            int errorCode = ReadIOs(startInput, inputLen, "inputs");
            if (errorCode != 0) return errorCode;

            errorCode = ReadIOs(startOutput, outpLen, "outputs");
            if (errorCode != 0) return errorCode;



            if (!_handler.WriteDiscreteControls(io, controls)) {
                Console.WriteLine($"Failed to write " +
                    $"{controls.Length} relays starting {io}.");
                return -3;
            }

            Console.WriteLine($"{controls.Length} relays starting {io} " +
                $"set to {string.Join(" ,", controls)}.");


            if (!_handler.ReadDiscreteIOs(io, controls.Length, out status)) {
                Console.WriteLine($"Failed to read status of " +
                    $"relays {controls.Length} starting {io}.");
                return -4;
            }

            if (status.Where((o) => o.State == SwitchSt.Unknown).Count() > 0) {

                Console.WriteLine($"Failed to write " +
                    $"{controls.Length} relays starting {io}." +
                    $"Some IOs have unknown status: " +
                    $"{string.Join(" ,", status.Select((o) => o.ToString()).ToArray())}.");
                return -5;
            }

            Console.WriteLine($"{controls.Length} relays " +
                $"starting {io} status is " +
                $"{string.Join(" ,", status.Select((o) => o.ToString()).ToArray())}.");

            errorCode = ReadIOs(startInput, inputLen, "inputs");
            if (errorCode != 0) return errorCode;

            errorCode = ReadIOs(startOutput, outpLen, "outputs");
            if (errorCode != 0) return errorCode;

            return 0;
        }

        private static int DoSingleIO(string io, int repeats = 4) {

            for (int i = 0; i < repeats; i++) {
                Thread.Sleep(100);
                SwitchCtrl ctrl = (i % 2) == 0 ? SwitchCtrl.On : SwitchCtrl.Off;

                if (!_handler.WriteDiscreteControl(io, ctrl)) {
                    return -1;
                }

                Console.WriteLine($"{io} relay set to {ctrl}.");
                Thread.Sleep(100);

                if (!_handler.ReadDiscreteIO(io, out SwitchState status)) {
                    return -2;
                }

                Console.WriteLine($"{io} relay status is {status}.");
            }
            return 0;
        }


        private static int DoSimpleTimerTest(string timer, string setRegister, string controlRelay, int timerTimeMs) {

            string timerCurrentValueRegister = timer.Replace("T", "TD");

            if (_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.Off)) {
                Console.WriteLine($"Control relay {controlRelay} switched off.");
            }
            else {
                Console.WriteLine($"Failed to switch control relay " +
                    $"{controlRelay} to off.");
                return -1;
            }

            if (_handler.ReadRegister(setRegister, out ushort timerValue)) {
                Console.WriteLine($"Current timer set value {timerValue}.");
            }
            else {
                Console.WriteLine($"Failed to read current set " +
                    $"value from {setRegister}.");
                return -2;
            }

            Thread.Sleep(1000);

            if (_handler.WriteRegister(setRegister, (ushort)timerTimeMs)) {
                Console.WriteLine($"Set timer value to {timerTimeMs}.");
            }
            else {
                Console.WriteLine($"Failed to set register " +
                    $"{setRegister} to {timerTimeMs}.");
                return -3;
            }

            if (_handler.ReadRegister(setRegister, out timerValue)) {
                Console.WriteLine($"Current timer set value: {timerValue}.");
            }
            else {
                Console.WriteLine($"Failed to read current " +
                    $"set value from {setRegister}.");
                return -4;
            }

            if (_handler.ReadRegister(timerCurrentValueRegister, out timerValue)) {
                Console.WriteLine($"Current timer counter " +
                    $"value: {timerValue}.");
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
                Console.WriteLine($"Control relay " +
                    $"{controlRelay} switched On.");
            }
            else {
                Console.WriteLine($"Failed to switch control " +
                    $"relay {controlRelay} to off.");
                return -1;
            }

            while (true) {

                if (!_handler.ReadDiscreteIO(timer, out switchState)) {
                    Console.WriteLine($"Failed to read timer " +
                        $"{timer} state.");
                    return -7;
                }

                if (!_handler.ReadRegister(timerCurrentValueRegister, out timerValue)) {
                    Console.WriteLine($"Failed to read timer " +
                        $"current value from {timerCurrentValueRegister}.");
                    return -8;
                }

                switch (switchState.State) {

                    case SwitchSt.On:
                        Console.WriteLine($"Timer tripped. Current " +
                            $"count value: {timerValue}.");
                        Thread.Sleep(5000);
                        if (_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.Off)) {
                            Console.WriteLine($"Control relay " +
                                $"{controlRelay} switched off.");


                            if (_handler.WriteRegister(setRegister, 0)) {
                                Console.WriteLine($"Timer set point cleared.");
                                Thread.Sleep(2500);
                                return 0;
                            }
                            else {
                                Console.WriteLine($"Failed to set timer " +
                                    $"set pint register {setRegister} to 0.");
                                return -12;
                            }

                        }
                        else {
                            Console.WriteLine($"Failed to switch " +
                                $"control relay {controlRelay} to off.");
                            return -9;
                        }


                    case SwitchSt.Off:
                        Console.WriteLine($"Timer is off at {timerValue} counts.");
                        break;

                    case SwitchSt.Unknown:
                        Console.WriteLine($"Error. Timer state is unknown " +
                            $"at {timerValue} counts.");

                        if (_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.Off)) {
                            Console.WriteLine($"Control relay {controlRelay} switched off.");
                        }
                        else {
                            Console.WriteLine($"Failed to switch control " +
                                $"relay {controlRelay} to off.");
                            return -11;
                        }
                        return -10;
                }
                Thread.Sleep(250);
            }
        }


        private static int DoSimpleCounterTest(string counter, string setValueRegister, string controlRelay, string resetRelay, int setPoint) {


            string counterCurrentValueRegister = counter.Replace("CT", "CTD");

            if (_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.Off)) {
                Console.WriteLine($"Control relay {controlRelay} " +
                    $"switched off.");
            }
            else {
                Console.WriteLine($"Failed to switch control " +
                    $"relay {controlRelay} to off.");
                return -1;
            }

            if (_handler.WriteRegister(setValueRegister, (ushort)setPoint)) {
                Console.WriteLine($"Counter set point set " +
                    $"to {(ushort)setPoint}.");
            }
            else {
                Console.WriteLine($"Failed to set counter set " +
                    $"point to {(ushort)setPoint}.");
                return -2;
            }

            if (_handler.ReadRegister(setValueRegister, out ushort setPointValue)) {
                Console.WriteLine($"Checking counter set value. " +
                    $"Readback says: {setPointValue}.");
            }
            else {
                Console.WriteLine($"Failed to read current set " +
                    $"value from {setValueRegister}.");
                return -3;
            }

            if (_handler.WriteDiscreteControl(resetRelay, SwitchCtrl.On)) {
                Console.WriteLine($"Reset relay {resetRelay} switched On.");
            }
            else {
                Console.WriteLine($"Failed to switch reset " +
                    $"relay {resetRelay} to On.");
                return -4;
            }

            Thread.Sleep(100);

            if (_handler.WriteDiscreteControl(resetRelay, SwitchCtrl.Off)) {
                Console.WriteLine($"Reset relay {resetRelay} switched Off.");
            }
            else {
                Console.WriteLine($"Failed to switch reset " +
                    $"relay {resetRelay} to Off.");
                return -5;
            }


            if (_handler.ReadRegister(counterCurrentValueRegister, out ushort counterValue)) {
                Console.WriteLine($"Current timer counter " +
                    $"value: {counterValue}.");
            }
            else {
                Console.WriteLine($"Failed to read current counter " +
                    $"value form {counterCurrentValueRegister}.");
                return -6;
            }


            if (_handler.ReadDiscreteIO(counter, out SwitchState switchState)) {
                Console.WriteLine($"Current counter output " +
                    $"state: {switchState.State.ToString()}.");
            }
            else {
                Console.WriteLine($"Failed to read current counter " +
                    $"output state value form {counter}.");
                return -7;
            }

            int ctr = 0;
            while (switchState.State != SwitchSt.On) {


                Console.Write($"Toggling counter input using " +
                    $"{controlRelay}. Iteration # {++ctr}. ");
                if (!_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.On)) {
                    Console.WriteLine($"Failed to switch control relay {controlRelay} to on.");
                    return -8;
                }

                Thread.Sleep(100);

                if (!_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.Off)) {
                    Console.WriteLine($"Failed to switch control relay {controlRelay} to off.");
                    return -9;
                }

                if (_handler.ReadDiscreteIO(counter, out switchState)) {
                    Console.WriteLine($"Current counter output state: {switchState}.");
                }
                else {
                    Console.WriteLine($"Failed to read current counter " +
                        $"output state value form {counter}.");
                    return -10;
                }
            }

            Console.WriteLine($"Counter {counter} reached set value {setPointValue}.");

            Thread.Sleep(5000);

            Console.WriteLine($"Resetting {counter} counter.");

            if (_handler.WriteDiscreteControl(resetRelay, SwitchCtrl.On)) {
                Console.WriteLine($"Reset relay {resetRelay} switched On.");
            }
            else {
                Console.WriteLine($"Failed to switch reset relay {resetRelay} to On.");
                return -4;
            }

            Thread.Sleep(5000);

            if (_handler.WriteDiscreteControl(resetRelay, SwitchCtrl.Off)) {
                Console.WriteLine($"Reset relay {resetRelay} switched Off.");
            }
            else {
                Console.WriteLine($"Failed to switch reset relay {resetRelay} to Off.");
                return -5;
            }



            return 0;

        }


        public static int ReadIOs(string startIO, int ioLen, string name) {

            if (!_handler.ReadDiscreteIOs(startIO, ioLen, out SwitchState[] status)) {
                Console.WriteLine($"Failed to read status of {ioLen} {name} starting {ioLen}.");
                return -1;
            }

            if (status.Where((o) => o.State == SwitchSt.Unknown).Count() > 0) {

                Console.WriteLine($"Failed to read status of {ioLen} the {name} starting {startIO}.  " +
                    $"Some IOs have unknown status: {string.Join(" ,", status.Select((o) => o.ToString()).ToArray())}.");
                return -2;
            }

            Console.WriteLine($"{ioLen} {name} starting {startIO} status is " +
                $"{string.Join(" ,", status.Select((o) => o.ToString()).ToArray())}.");

            return 0;

        }

    }
}
