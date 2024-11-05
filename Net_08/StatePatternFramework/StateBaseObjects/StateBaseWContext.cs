using Microsoft.Extensions.Logging;

namespace Grumpy.StatePatternFramework
{
    public abstract class StateBaseWContext<TContext> : StateBase
        where TContext : StateMachineBase
    {
        public StateBaseWContext(TContext? context, StateIDBase en,
            int period = InfiniteTimeout, ILogger? logger = null) :
            base(context, en, period, logger) { }

        public new TContext? Context => base.Context as TContext;
    }
}
