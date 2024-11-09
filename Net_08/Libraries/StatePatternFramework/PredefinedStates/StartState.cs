
namespace Grumpy.StatePatternFramework
{
    public class StartState : StateBase
    {
        public StartState(StateMachineBase context) : 
            base(context, StateIDBase.Start) { }

        public override void StateProc(StateProcArgs args)
        {
            Result = StateExecutionResult.Completed;
        }
    }
}
