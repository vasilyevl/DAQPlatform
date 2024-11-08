
namespace Grumpy.StatePatternFramework
{
    public class EndState : StateBase
     
    {
        public EndState(StateMachineBase context) : 
                            base(context, StateIDBase.End) { }

        public override void StateProc(StateProcArgs args)
        {
            Result = StateExecutionResult.Completed;
        }
    }
}
