
namespace FSM
{
    public class StartState : StateBase
    {
        public StartState( FSMBase device) : base(StateEnumBase.Start) { }

        public override void StateProc(StateProcArgs args)
        {
            StateStatus = FsmStateStatus.Complete;
        }
    }
}
