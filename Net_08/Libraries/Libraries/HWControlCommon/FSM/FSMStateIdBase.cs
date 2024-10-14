
using Grumpy.Common;

namespace Grumpy.FSM
{
    public class FSMStateIdBase: EnumBase
    {
        public static readonly FSMStateIdBase Unknown = new FSMStateIdBase("Unknown", 0);
        public static readonly FSMStateIdBase Start = new FSMStateIdBase("Start", 1);
        public static readonly FSMStateIdBase Stop = new FSMStateIdBase("Stop", 2);
        public static readonly FSMStateIdBase Pause = new FSMStateIdBase("Pause", 3);
        public static readonly FSMStateIdBase Resume = new FSMStateIdBase("Resume", 4);
        public static readonly FSMStateIdBase End = new FSMStateIdBase("End", 5);
        protected FSMStateIdBase(string name, int id) : base(name, id) {


        }
    }
}
