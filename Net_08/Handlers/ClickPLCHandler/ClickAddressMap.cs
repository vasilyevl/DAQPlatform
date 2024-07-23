/*
 * Copyright (c) 2024 Grumpy. Permission is hereby granted, 
free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"),to deal in the Software 
without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the 
Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*/


namespace Grumpy.ClickPLCHandler
{
    public enum FunctionCode
    {
        ReadSingleCoil = 1,
        ReadSingleInput = 2,
        ReadHoldingRegisters = 3,
        ReadInternalRegisters = 4,
        WriteSingleCoil = 5,
        WriteSingleRegister = 6,
        WriteMultipleCoils = 15,
        WriteMultipleRegisters = 16,
        MaskedWriteRegister = 22
    }

    public enum IoFunction
    {
        SingleControlRead,
        MultipleControlRead,
        SingleControlWrite,
        MultipleControlWrite
    }

    public class ModBusAddress
    {

        private List<FunctionCode> _functionCodes = new List<FunctionCode>();

        public ModBusAddress(int address, int readCode = -1, int writeCode = -1,
                              int readMultipleCode = -1, int writeMultpleCode = -1) {

            Address = address;
            ReadSingleCode = readCode;
            WriteSingleCode = writeCode;
            ReadMultipleCode = readMultipleCode;
            WriteMultipleCode = writeMultpleCode;
        }

        public int Address {
            get;
            private set;
        }

        public int ReadSingleCode { get; private set; }
        public int WriteSingleCode { get; private set; }


        private int _writeMultipleCode = -1;
        public int WriteMultipleCode {
            get => _writeMultipleCode < 1 ? WriteSingleCode : _writeMultipleCode;
            private set => _writeMultipleCode = value;
        }

        private int _readMultipleCode = -1;
        public int ReadMultipleCode {
            get => _readMultipleCode < 1 ? ReadSingleCode : _readMultipleCode;
            private set => _readMultipleCode = value;
        }
    }

    public static class ClickAddressMap
    {
        public const int XStartAddress984 = 100000;
        public const int XStartAddressHex = 0x0;
        public const int YStartAddress984 = 8192;
        public const int YStartAddressHex = 0x2000;
        public const int CStartAddress984 = 16384;
        public const int CStartAddressHex = 0x4000;
        public const int TStartAddress984 = 145056;
        public const int TStartAddressHex = 0xB000;
        public const int CTStartAddress984 = 149152;
        public const int CTStartAddressHex = 0xC000;
        public const int SCStartAddress984 = 161440;
        public const int SCStartAddressHex = 0xF000;
        public const int DSStartAddress984 = 400000;
        public const int DSStartAddressHex = 0x0000;
        public const int DDStartAddress984 = 416384;
        public const int DDStartAddressHex = 0x4000;
        public const int DHStartAddress984 = 424576;
        public const int DHStartAddressHex = 0x6000;
        public const int DFStartAddress984 = 428672;
        public const int DFStartAddressHex = 0x7000;
        public const int XDStartAddress984 = 357344;
        public const int XDStartAddressHex = 0xE000;
        public const int XDUStartAddress984 = 357345;
        public const int XDUStartAddressHex = 0xE001;
        public const int YDStartAddress984 = 457856;
        public const int YDStartAddressHex = 0xE200;
        public const int YDUStartAddress984 = 457857;
        public const int YDUStartAddressHex = 0xE201;
        public const int TDStartAddress984 = 445056;
        public const int TDStartAddressHex = 0xB000;
        public const int CTDStartAddress984 = 449152;
        public const int CTDStartAddressHex = 0xC000;
        public const int SDStartAddress984 = 361440;
        public const int SDStartAddressHex = 0xF000;
        public const int TxtStartAddress984 = 436864;
        public const int TxtStartAddressHex = 0x9000;

        private static readonly Dictionary<IOType, int> ModBusRtuAddress =
            new Dictionary<IOType, int>()
            {
                { IOType.Input, XStartAddress984 },
                { IOType.Output, YStartAddress984 },
                { IOType.ControlRelay , CStartAddress984 },
                { IOType.Timer, TStartAddress984 },
                { IOType.Counter, CTStartAddress984 },
                { IOType.SystemControlRelay, SCStartAddress984 },
                { IOType.RegisterInt16, DSStartAddress984 },
                { IOType.RegisterInt32, DDStartAddress984 },
                { IOType.RegisterHex, DHStartAddress984 },
                { IOType.RegisterFloat32, DFStartAddress984 },
                { IOType.InputRegister, XDStartAddress984 },
                { IOType.OutputRegister, YDStartAddress984 },
                { IOType.TimerRegister, TDStartAddress984 },
                { IOType.CounterRegister, CTDStartAddress984 },
                { IOType.SystemRegister, SDStartAddress984 },
                { IOType.Text, TxtStartAddress984 }
            };

        private static readonly Dictionary<IOType, ModBusAddress> ModBusHexAddress =
            new Dictionary<IOType, ModBusAddress>()
            {
                { IOType.Input, new ModBusAddress( XStartAddressHex,
                    readCode: (int) FunctionCode.ReadSingleInput)},

                { IOType.Output, new ModBusAddress(YStartAddressHex,
                    readCode: (int) FunctionCode.ReadSingleCoil,
                    writeCode: (int) FunctionCode.WriteSingleCoil,
                    writeMultpleCode: (int) FunctionCode.WriteMultipleCoils) },

                { IOType.ControlRelay , new ModBusAddress( CStartAddressHex,
                    readCode: (int) FunctionCode.ReadSingleCoil,
                    writeCode: (int) FunctionCode.WriteSingleCoil,
                    writeMultpleCode: (int) FunctionCode.WriteMultipleCoils) },

                { IOType.Timer, new ModBusAddress(TStartAddressHex,
                    readCode: (int) FunctionCode.ReadSingleInput) },


                { IOType.Counter, new ModBusAddress(CTStartAddressHex,
                    readCode :(int) FunctionCode.ReadSingleInput) },

                { IOType.SystemControlRelay, new ModBusAddress(SCStartAddressHex,
                    readCode :(int) FunctionCode.ReadSingleInput) },

                { IOType.RegisterInt16, new ModBusAddress(DSStartAddressHex,
                    readCode :(int) FunctionCode.ReadHoldingRegisters,
                    writeCode : (int) FunctionCode.WriteSingleRegister,
                    writeMultpleCode :(int) FunctionCode.WriteMultipleRegisters) },

                { IOType.RegisterInt32, new ModBusAddress(DDStartAddressHex,
                    readCode :(int) FunctionCode.ReadHoldingRegisters,
                    writeCode : (int) FunctionCode.WriteSingleRegister,
                    writeMultpleCode :(int) FunctionCode.WriteMultipleRegisters) },

                { IOType.RegisterHex, new ModBusAddress(DHStartAddressHex,
                    readCode :(int) FunctionCode.ReadHoldingRegisters,
                    writeCode : (int) FunctionCode.WriteSingleRegister,
                    writeMultpleCode :(int) FunctionCode.WriteMultipleRegisters) },

                { IOType.RegisterFloat32, new ModBusAddress(DFStartAddressHex,
                    readCode :(int) FunctionCode.ReadHoldingRegisters,
                    writeCode : (int) FunctionCode.WriteSingleRegister,
                    writeMultpleCode :(int) FunctionCode.WriteMultipleRegisters) },

                { IOType.InputRegister, new ModBusAddress(XDStartAddressHex,
                    readCode: (int) FunctionCode.ReadInternalRegisters) },

                { IOType.OutputRegister, new ModBusAddress(YDStartAddressHex,
                    readCode :(int) FunctionCode.ReadHoldingRegisters,
                    writeCode : (int) FunctionCode.WriteSingleRegister,
                    writeMultpleCode :(int) FunctionCode.WriteMultipleRegisters) },

                { IOType.TimerRegister, new ModBusAddress(TDStartAddressHex,
                    readCode :(int) FunctionCode.ReadHoldingRegisters,
                    writeCode : (int) FunctionCode.WriteSingleRegister,
                    writeMultpleCode :(int) FunctionCode.WriteMultipleRegisters) },

                { IOType.CounterRegister, new ModBusAddress(CTDStartAddressHex,
                    readCode :(int) FunctionCode.ReadHoldingRegisters,
                    writeCode : (int) FunctionCode.WriteSingleRegister,
                    writeMultpleCode :(int) FunctionCode.WriteMultipleRegisters) },

                { IOType.SystemRegister, new ModBusAddress(SDStartAddressHex,
                    readCode: (int) FunctionCode.ReadInternalRegisters) },

                { IOType.Text, new ModBusAddress(TxtStartAddressHex,
                    readCode :(int) FunctionCode.ReadHoldingRegisters,
                    writeCode : (int) FunctionCode.WriteSingleRegister,
                    writeMultpleCode :(int) FunctionCode.WriteMultipleRegisters) },
            };

        private static IReadOnlyDictionary<string, IOType> _ioTypes =
            new Dictionary<string, IOType>() {
                {"X", IOType.Input },
                {"Y", IOType.Output},
                {"C", IOType.ControlRelay},
                {"T", IOType.Timer},
                {"CT", IOType.Counter},
                {"SC", IOType.SystemControlRelay},
                {"DS", IOType.RegisterInt16},
                {"DD", IOType.RegisterInt32},
                {"DH", IOType.RegisterHex},
                {"DF", IOType.RegisterFloat32},
                {"XD", IOType.InputRegister},
                {"XDU", IOType.InputRegister },
                {"YD", IOType.OutputRegister},
                {"YDU", IOType.OutputRegister },
                {"TD", IOType.TimerRegister},
                {"CTD", IOType.CounterRegister},
                {"SD", IOType.SystemRegister},
                {"TXT", IOType.Text}
            };

        private static ClickErrorCode _GetModBusHexAddress(IoFunction function,
            IOType type, out int address, out int fuctionCode) {

            address = -1;
            fuctionCode = -1;

            if (ModBusHexAddress.ContainsKey(type)) {

                address = ModBusHexAddress[type].Address;

                switch (function) {

                    case IoFunction.SingleControlRead:
                        fuctionCode = ModBusHexAddress[type].ReadSingleCode;
                        break;

                    case IoFunction.MultipleControlRead:
                        fuctionCode = ModBusHexAddress[type].ReadMultipleCode;
                        break;

                    case IoFunction.SingleControlWrite:
                        fuctionCode = ModBusHexAddress[type].WriteSingleCode;
                        break;

                    case IoFunction.MultipleControlWrite:
                        fuctionCode = ModBusHexAddress[type].WriteMultipleCode;
                        break;
                }

                return ClickErrorCode.NoError;
            }
            else {

                return ClickErrorCode.IoNotSupported;
            }
        }

        public static ClickErrorCode GetModBusHexAddress(IoFunction ioFunction, string control,
            out int address, out int functionCode) {
            address = -1;
            functionCode = -1;

            ClickErrorCode err = _DecodeControlName(control,
                out IOType type, out int nameAddress);

            if (err == ClickErrorCode.NoError) {

                err = _GetModBusHexAddress(ioFunction, type,
                    out int baseAddress, out functionCode);

                address = (err == ClickErrorCode.NoError)
                    ? baseAddress + nameAddress
                    : -1;
            }

            return err;
        }

        public static List<string> ValidControlNamePreffixes =>
            new List<string>(_ioTypes.Keys);

        private static int _RegisterSize(IOType type) {
            switch (type) {

                case (IOType.RegisterInt32):
                case (IOType.RegisterFloat32):
                    return 2;
                default:
                    return 1;
            }
        }

        private static bool _IsRegister(IOType type) {
            return type == IOType.RegisterInt16
                || type == IOType.RegisterFloat32
                || type == IOType.RegisterHex
                || type == IOType.RegisterInt32
                || type == IOType.InputRegister
                || type == IOType.OutputRegister
                || type == IOType.TimerRegister
                || type == IOType.CounterRegister
                || type == IOType.SystemRegister;
        }

        private static bool _IsText(IOType type) {
            return type == IOType.Text;
        }

        private static ClickErrorCode _DecodeControlName(string name, out IOType ioType, out int nameAddress) {
            ioType = IOType.Unknown;
            nameAddress = -1;

            var prefixes = ValidControlNamePreffixes
                .Where((x) => name.ToUpper().StartsWith(x.ToUpper()))
                .OrderByDescending((x) => x.Length);


            if ((prefixes?.Count() ?? 0) < 1) {

                return ClickErrorCode.InvalidControlNamePrefix;
            }

            String? prefix = prefixes?.First() ?? null;

            if ((prefix != null) && (_ioTypes?.ContainsKey(prefix) ?? false)) {

                ioType = _ioTypes[prefix];
            }
            else {

                return ClickErrorCode.InvalidControlNamePrefix;
            }


            try {

                int idx = name.IndexOf(prefix);

                if (idx <= name.Length) {

                    var s = name.ToUpper().Substring(idx + prefix.Length);
                    nameAddress = Int32.Parse(s) - 1;
                }
                else {

                    nameAddress = 0;
                }

                return ClickErrorCode.NoError;
            }
            catch {

                nameAddress = -1;
                return ClickErrorCode.InvalidControlName;
            }
        }
    }
}
