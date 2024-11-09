using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.StatePatternFramework
{
    public class StopState : StateBase      
    {
        public StopState(StateMachineBase context) : 
                    base(context, StateIDBase.Stop) { }

        public override void StateProc(StateProcArgs args)
        {
            Result = StateExecutionResult.Completed;
        }
    }
}
