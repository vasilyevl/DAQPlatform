using System;
using System.Collections.Generic;
using System.Linq;

namespace LV.ClickPLCHandler
{
    public static class ClickAddressMap
    {
        public const int XStartAddress984 = 100000;
        public const int XStartAddressHex = 0x0;
        public const int YStartAddress984 = 8192;
        public const int YStartAddressHex = 2000;
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
        public const int YDStartAddress984 = 457856;
        public const int YDStartAddressHex = 0xE200;
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

        private static readonly Dictionary<IOType, int> ModBusHexAddress =
            new Dictionary<IOType, int>()
            {
                { IOType.Input, XStartAddressHex },
                { IOType.Output, YStartAddressHex },
                { IOType.ControlRelay , CStartAddressHex },
                { IOType.Timer, TStartAddressHex },
                { IOType.Counter, CTStartAddressHex },
                { IOType.SystemControlRelay, SCStartAddressHex },
                { IOType.RegisterInt16, DSStartAddressHex },
                { IOType.RegisterInt32, DDStartAddressHex },
                { IOType.RegisterHex, DHStartAddressHex },
                { IOType.RegisterFloat32, DFStartAddressHex },
                { IOType.InputRegister, XDStartAddressHex },
                { IOType.OutputRegister, YDStartAddressHex },
                { IOType.TimerRegister, TDStartAddressHex },
                { IOType.CounterRegister, CTDStartAddressHex },
                { IOType.SystemRegister, SDStartAddressHex },
                { IOType.Text, TxtStartAddressHex }
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
                {"YD", IOType.OutputRegister},
                {"TD", IOType.TimerRegister},
                {"CTD", IOType.CounterRegister},
                {"SD", IOType.SystemRegister},
                {"TXT", IOType.Text}
            };

        public static ErrorCode GetModBaseAddress(out int address, IOType type, bool rtu = false)
        {
            address = -1;
            var lookUpTable = rtu ? ModBusRtuAddress : ModBusHexAddress;
            if (lookUpTable.ContainsKey(type))
            {
                address = lookUpTable[type];
                return ErrorCode.NoError;
            }
            else
            {
                return ErrorCode.IoNotSupported;
            }
        }

        public static ErrorCode GetModAddress(out int address, string control, bool rtu = false)
        {
            address = -1;

            var err = _DecodeControlName(control, out IOType type, out int nameAddress);

            if (err == ErrorCode.NoError)
            {

                var lookUpTable = rtu ? ModBusRtuAddress : ModBusHexAddress;

                if (lookUpTable.ContainsKey(type))
                {

                    address = lookUpTable[type] + nameAddress;
                    return ErrorCode.NoError;
                }
            }

            return err;
        }

        public static List<string> ValidControlNamePreffixes =>
            new List<string>(_ioTypes.Keys);

        private static int _RegisterSize(IOType type)
        {
            switch (type)
            {

                case (IOType.RegisterInt32):
                case (IOType.RegisterFloat32):
                    return 2;
                default:
                    return 1;
            }
        }

        private static bool _IsRegister(IOType type)
        {
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

        private static bool _IsText(IOType type)
        {
            return type == IOType.Text;
        }

        private static ErrorCode _DecodeControlName(string name, out IOType ioType, out int nameAddress)
        {
            ioType = IOType.Unknown;
            nameAddress = -1;

            var preffix =
                ValidControlNamePreffixes
                .FirstOrDefault((x) => name.ToUpper().StartsWith(x.ToUpper()));

            if (string.IsNullOrEmpty(preffix))
            {
                return ErrorCode.InvalidControlName;
            }

            if (_ioTypes.ContainsKey(preffix))
            {
                ioType = _ioTypes[preffix];
            }
            else
            {
                return ErrorCode.InvalidControlNamePreffix;
            }


            try
            {
                int idx = name.IndexOf(preffix);
                if (idx <= name.Length)
                {
                    nameAddress = Int32.Parse(name.ToUpper().Substring(idx + preffix.Length)) - 1;
                }
                else
                {
                    nameAddress = 0;
                }

                return ErrorCode.NoError;
            }
            catch
            {
                nameAddress = -1;
                return ErrorCode.InvalidControlName;
            }
        }
    }
}
