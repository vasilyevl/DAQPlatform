
namespace FSM
{
    public abstract class FsmStateBase<TContext> : StateBase
    {
        public FsmStateBase( TContext context,
                                StateEnumBase id, 
                                int periodMs = InfiniteTimeout) : 
                                        base(id, periodMs) 
        {
            Context = context;
        }

        public  void ClearError() {
            LastError = string.Empty;
        }

        public string LastError
        { get; protected set;}

        public TContext Context
        { get; private set; }

    }
}
