using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSM
{
    public class FSM_ErrorStateBase : StateBase
    {
        public FSM_ErrorStateBase(FSMBase device ) : base(StateEnumBase.GenericError) { }

        public override void StateProc(StateProcArgs args)
        {
            StateStatus = FsmStateStatus.Complete;
        }
    }
}
