using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.StatePatternFramework
{
    public class FSM_ErrorStateBase<TContext> : StateBaseWContext<TContext>
        where TContext : StateMachineBase
    {
        public FSM_ErrorStateBase(TContext context) : 
            base(context, StateIDBase.GenericError) { }

        public override void StateProc(StateProcArgs args)
        {
            Result = StateExecutionResult.Completed;
        }
    }
}
