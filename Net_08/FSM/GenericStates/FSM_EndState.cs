
namespace FSM
{
    public class EndState : StateBase
    {
        public EndState(FSMBase device) : base(StateEnumBase.End) { }

        public override void StateProc(StateProcArgs args)
        {
            StateStatus = FsmStateStatus.Complete;
        }
    }
}
