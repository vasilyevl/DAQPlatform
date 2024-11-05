using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSM
{
    public class StopState: StateBase
    {
        public StopState(FSMBase device) : base(StateEnumBase.Stop) { }

        public override void StateProc(StateProcArgs args)
        {
            StateStatus = FsmStateStatus.Complete;
        }
    }
}
