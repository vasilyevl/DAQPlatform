using Grumpy.ClickPLC;
using Grumpy.HWControl.IO;
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
        static ClickHandler _handler = null!;
        static string _io = "C1";
        static string _timer = "T1";
        static string _setPoint = "DS10";
        static string _controlRelay = "C16";
        static int _setTimeMs = 2500;

        static int Main(string[] args) {
            int exitCode = 0;

            string config = "{\"Interface\":{" +
                                             "\"Selector\":\"Network\"," +
                                             "\"SerialPort\":null," +
                                             "\"Network\":{\"Name\":\"ClickPLC\"," +
                                                          "\"IpAddress\":\"192.168.1.22\"," +
                                                          "\"Port\":502," +
                                                          "\"Timeout\":15000}}}";

            if (!ClickHandler.CreateHandler(config, out _handler!)) {
                Console.WriteLine("Failed to create handler.");
                return -1;
            }

            if (!_handler.Open()) {

                exitCode = -1;
                Console.WriteLine("Failed to open handler.");
                return exitCode;
            }


            Console.WriteLine("Handler opened.");



            if ((exitCode = DoSingleIO(_io)) != 0) {
                goto Exit;
            }


            if ((exitCode = DoMultipleRelaysTest("C2", 8)) != 0) {

                Console.WriteLine($"Discrete IO array test failed with exit " +
                    $"code {exitCode} .");
                goto Exit;
            }

            Thread.Sleep(250);

            if ((exitCode = DoSimpleCounterTest("CT1", "DS11", "C10", "C11", 25)) != 0) {
                goto Exit;
            }

            if ((exitCode = DoSimpleTimerTest(_timer, _setPoint,
                _controlRelay, _setTimeMs)) != 0) {
                goto Exit;
            }


            if ((exitCode = FloatRegisterTest("DF10", 22.11f)) != 0) {              
                goto Exit;
            }



            if ((exitCode = AIOTest()) != 0) {

                goto Exit;
            }

            Exit:
            Console.WriteLine($"\n\n All Tests complete with error code {exitCode}");
            Console.WriteLine("Click 'Enter' to exit.");
            Console.ReadLine();

            return exitCode;
        }

        private static int DoMultipleRelaysTest(string startRelay, int quantity) {


            Console.WriteLine("\n\nStarting discrete IO array  read/write test.\n");
            SwitchCtrl[] controlArray = new SwitchCtrl[quantity];

            controlArray = controlArray.Select((x) => SwitchCtrl.On).ToArray();

            int errorCode;
            if ((errorCode = DoDiscreteIOs(startRelay, controlArray)) != 0) {
                return errorCode;
            }

            int cntr = 0;
            controlArray = controlArray.Select((x) => (cntr++ % 2 == 0 ? SwitchCtrl.Off : SwitchCtrl.On)).ToArray();
            Thread.Sleep(200);
            if ((errorCode = DoDiscreteIOs(startRelay, controlArray)) != 0) {
                return errorCode;
            }

            cntr = 0;
            controlArray = controlArray.Select((x) => (cntr++ % 2 == 0 ? SwitchCtrl.On : SwitchCtrl.Off)).ToArray();
            Thread.Sleep(200);
            if ((errorCode = DoDiscreteIOs(startRelay, controlArray)) != 0) {
                return errorCode;
            }
            Thread.Sleep(200);
            controlArray = controlArray.Select((x) => SwitchCtrl.Off).ToArray();
            errorCode = DoDiscreteIOs(startRelay, controlArray);

            if ( errorCode == 0) {
                Console.WriteLine("\nDiscrete IO array read write test complete.\n");
            }
            else {
                Console.WriteLine("\nDiscrete IO array read/write test failed.\n");
            }
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
            Console.WriteLine("\n\nStarting simple single discrete IO test.\n");
            for (int i = 0; i < repeats; i++) {
                Thread.Sleep(100);
                SwitchCtrl ctrl = (i % 2) == 0 ? SwitchCtrl.On : SwitchCtrl.Off;

                if (!_handler.WriteDiscreteControl(io, ctrl)) {
                    Console.WriteLine($"Simple single discrete IO test failed");
                    return -1;
                }

                Console.WriteLine($"{io} relay set to {ctrl}.");
                Thread.Sleep(100);

                if (!_handler.ReadDiscreteIO(io, out SwitchState status)) {
                    return -2;
                }

                Console.WriteLine($"{io} relay status is {status}.");
            }
            Console.WriteLine("\nSimple single discrete IO test complete.");
            return 0;
        }

        private static int DoSimpleTimerTest(string timer, string setRegister, string controlRelay, int timerTimeMs) {

            Console.WriteLine("\n\nStarting timer test.\n");
            string timerCurrentValueRegister = timer.Replace("T", "TD");

            if (_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.Off)) {
                Console.WriteLine($"Control relay {controlRelay} switched off.");
            }
            else {
                Console.WriteLine($"Timer test error. Failed to switch control relay " +
                    $"{controlRelay} to off.");
                return -1;
            }

            if (_handler.ReadRegister(setRegister, out ushort timerValue)) {
                Console.WriteLine($"Current timer set value {timerValue}.");
            }
            else {
                Console.WriteLine($"Timer test error. Failed to read current set " +
                    $"value from {setRegister}.\n");
                return -2;
            }

            Thread.Sleep(200);

            if (_handler.WriteRegister(setRegister, (ushort)timerTimeMs)) {
                Console.WriteLine($"Set timer value to {timerTimeMs}.");
            }
            else {
                Console.WriteLine($"Timer test error. Failed to set register " +
                    $"{setRegister} to {timerTimeMs}.\n");
                return -3;
            }

            if (_handler.ReadRegister(setRegister, out timerValue)) {
                Console.WriteLine($"Current timer set value: {timerValue}.");
            }
            else {
                Console.WriteLine($"Timer test error. Failed to read current " +
                    $"set value from {setRegister}.\n");
                return -4;
            }

            if (_handler.ReadRegister(timerCurrentValueRegister, out timerValue)) {
                Console.WriteLine($"Current timer counter " +
                    $"value: {timerValue}.");
            }
            else {
                Console.WriteLine($"Timer test error. Failed to read current timer " +
                    $"counter value form {timerCurrentValueRegister}.\n");
                return -5;
            }


            if (_handler.ReadDiscreteIO(timer, out SwitchState switchState)) {
                Console.WriteLine($"Current timer output state: {switchState}.");
            }
            else {
                Console.WriteLine($"Timer test error. Failed to read current timer " +
                    $"output state value form {timer}.\n");
                return -6;
            }

            if (_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.On)) {
                Console.WriteLine($"Control relay " +
                    $"{controlRelay} switched On.");
            }
            else {
                Console.WriteLine($"Timer test error. Failed to switch control " +
                    $"relay {controlRelay} to off.\n");
                return -1;
            }

            while (true) {

                if (!_handler.ReadDiscreteIO(timer, out switchState)) {
                    Console.WriteLine($"Timer test error. Failed to read timer " +
                        $"{timer} state.\n");
                    return -7;
                }

                if (!_handler.ReadRegister(timerCurrentValueRegister, out timerValue)) {
                    Console.WriteLine($"Timer test error. Failed to read timer " +
                        $"current value from {timerCurrentValueRegister}.\n");
                    return -8;
                }

                switch (switchState.State) {

                    case SwitchSt.On:
                        Console.WriteLine($"Timer tripped. Current " +
                            $"count value: {timerValue}.");
                        Thread.Sleep(200);
                        if (_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.Off)) {
                            Console.WriteLine($"Control relay " +
                                $"{controlRelay} switched off.");


                            if (_handler.WriteRegister(setRegister, 0)) {
                                Console.WriteLine($"Timer set point cleared.");
                                Thread.Sleep(2500);
                                Console.WriteLine("\nTimer test complete.\n");
                                return 0;
                            }
                            else {
                                Console.WriteLine($"Timer test error. Failed to set timer " +
                                    $"set pint register {setRegister} to 0.\n");
                                return -12;
                            }
                        }
                        else {
                            Console.WriteLine($"Timer test error. Failed to switch " +
                                $"control relay {controlRelay} to off.\n");
                            return -9;
                        }

                    case SwitchSt.Off:
                        Console.WriteLine($"Timer is off at {timerValue} counts.");
                        break;

                    case SwitchSt.Unknown:
                        Console.WriteLine($"Error. Timer state is unknown " +
                            $"at {timerValue} counts.\n");

                        if (_handler.WriteDiscreteControl(controlRelay, SwitchCtrl.Off)) {
                            Console.WriteLine($"Control relay {controlRelay} switched off.");
                        }
                        else {
                            Console.WriteLine($"Timer test error. Failed to switch control " +
                                $"relay {controlRelay} to off.\n");
                            return -11;
                        }
                        return -10;
                }
                Thread.Sleep(100);
            }

        }

        private static int DoSimpleCounterTest(string counter, string setValueRegister,
            string controlRelay, string resetRelay, int setPoint) {

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

            Thread.Sleep(100);

            Console.WriteLine($"Resetting {counter} counter.");

            if (_handler.WriteDiscreteControl(resetRelay, SwitchCtrl.On)) {
                Console.WriteLine($"Reset relay {resetRelay} switched On.");
            }
            else {
                Console.WriteLine($"Failed to switch reset relay {resetRelay} to On.");
                return -4;
            }

            Thread.Sleep(100);

            if (_handler.WriteDiscreteControl(resetRelay, SwitchCtrl.Off)) {
                Console.WriteLine($"Reset relay {resetRelay} switched Off.");
            }
            else {
                Console.WriteLine($"Failed to switch reset relay {resetRelay} to Off.");
                return -5;
            }

            Console.WriteLine($"\nCounter test complete.\n");
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


        internal static int AIOTest() {

            string floatRegisterAD1 = "DF1";
            string floatRegisterAD2 = "DF2";
            string FloatRegisterDA1 = "DF3";
            string FloatRegisterDA2 = "DF4";

            float testValueMax = 4095;
            float testValueMin = 0;

            int steps = 11;
            float increment = (steps <= 1) ?
                (testValueMax - testValueMin)
                : (testValueMax - testValueMin) / (steps - 1);

            Console.WriteLine("\n\nStarting Analog IO test.\n");

            for (int i = 0; i < steps; i++) {

                float da1WriteValue =
                    testValueMin + ((steps > 1) ? increment * i : increment);
                float da2WriteValue =
                    testValueMax - ((steps > 1) ? increment * i : increment);



                if (!_handler.WriteFloatRegister(FloatRegisterDA1, da1WriteValue)) {

                    Console.WriteLine($"AIO test. Failed to write float to Click PLC " +
                        $"register {FloatRegisterDA1}.");
                    return -15;
                }

                Console.WriteLine($"{DateTime.Now.ToString("yy/MM/dd HH:mm:ss:fff")} " +
                    $"New AO1 and AO2 set values: {da1WriteValue}, {da2WriteValue}.");
                if (!_handler.WriteFloatRegister(FloatRegisterDA2, da2WriteValue)) {

                    Console.WriteLine($"AIO test. Failed to write float to Click PLC " +
                        $"register {FloatRegisterDA1}.");
                    return -15;
                }

                Thread.Sleep(50);

                if (!_handler.ReadFloatRegister(floatRegisterAD1, out float readValue1)) {

                    Console.WriteLine($"AIO test. Failed to read float from Click PLC " +
                        $"register {floatRegisterAD1}.");
                    return -16;
                }



                if (!_handler.ReadFloatRegister(floatRegisterAD2, out float readValue2)) {
                    Console.WriteLine($"AIO test. Failed to read float from Click PLC " +
                        $"register {floatRegisterAD2}.");
                    return -16;
                }

                Console.WriteLine($"{DateTime.Now.ToString("yy/MM/dd HH:mm:ss:fff")} " +
                    $"New AI1 and AI2 values: {readValue1}, {readValue2}.");

            }
            Console.WriteLine("Analog IO test complete.");
            return 0;

        }
        internal static int FloatRegisterTest(string name, float value) {


            Console.WriteLine($"\n\nFloat register test started.\n");

            for (int i = 0; i < 10; i++) {
                if (!_handler.WriteFloatRegister(name, value + i)) {

                    Console.WriteLine($"Float register test error. Failed to write float register {name} with value {value}.\n");
                    return -11;
                }

                Console.WriteLine($"Wrote value {value+i} to float register {name}.");

                if (!_handler.ReadFloatRegister(name, out float readValue)) {

                    Console.WriteLine($"Float register test error. Failed to read float register {name}.\n");
                    return -12;
                }

                Console.WriteLine($"Read value {readValue} from float register {name}.");

                if (Math.Abs(value + i - readValue) > 0.001) {

                    Console.WriteLine($"Float register test error. Read value {readValue} from float register {name} " +
                                           $"does not match written value {value}.\n");
                    return -13;
                }
            }
            Console.WriteLine($"\nFloat register test complete.\n");
            return 0;

        }
    }
}
