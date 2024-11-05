using Grumpy.DaqFramework.Common;

namespace Grumpy.StatePatternFramework
{
    public class CommandTypeBase : EnumBase
    {
        public CommandTypeBase(string name, int id) : base(name, id) { }

        public static readonly CommandTypeBase Generic = new CommandTypeBase("Generic", 0);
        public static readonly CommandTypeBase Open = new CommandTypeBase("Open", 1);
        public static readonly CommandTypeBase Close = new CommandTypeBase("Close", 2);
        public static readonly CommandTypeBase Reset = new CommandTypeBase("Reset", 3);
        public static readonly CommandTypeBase ApplySettings = new CommandTypeBase("ApplySettings", 4);
    }
}
