using Microsoft.Extensions.Logging;

namespace Grumpy.StatePatternFramework
{
    public class StateTransitionManager 
    {
        protected ILogger? _logger = null;

        private Dictionary<TransitionTrigger, StateBase> _transitions;

        public StateTransitionManager(ILogger logger = null!) {
            _logger = logger;
            _transitions = [];
        }

        public bool LoggerIsSet => _logger != null; 

        public bool AddTransition(StateBase initialState, 
                                  StateExecutionResult stateExitStatus, 
                                  StateBase nextState)
        {
            var transitionTrigger = 
                new TransitionTrigger(initialState, stateExitStatus);

            return AddTransition(transitionTrigger, nextState);
        }

        public bool AddTransition(TransitionTrigger transitionTrigger, 
                                  StateBase nextState)
        {

            if (!_transitions.ContainsKey(transitionTrigger)) {

                _transitions.Add(transitionTrigger, nextState);
                return true;
            }

           _logger?.LogWarning($"State Transition Manager. " +
                $"Attempt to add redundant transition to the state " +
                $"{nextState.Name}:\nexecution result " +
                $"{transitionTrigger.Status} to state " +
                $"{_transitions[transitionTrigger].Name}. " +
                $"Request ignored.");

            return false;
        }

        public bool ContainsTrigger(TransitionTrigger transitionTrigger)
        {
            return _transitions.ContainsKey(transitionTrigger);
        }

        public bool PeekNextState(TransitionTrigger trigger, out StateBase? nextState)
        {       
            if (_transitions.ContainsKey(trigger)) {

                nextState = _transitions[trigger];
            }
            else {

                nextState = null;
            }

            return nextState is not null;
        }

        public bool PeekNextStateName(TransitionTrigger trigger, 
                                      out string nextStateName)
        {            
                nextStateName = 
                    PeekNextState(trigger, out StateBase? nextState) ?
                        nextState?.Name ?? string.Empty : 
                        string.Empty;
   
            return nextStateName != string.Empty;
        }

        public StateBase? NextState(StateBase st)
        {
            TransitionTrigger trigger = new TransitionTrigger(st, st.Result);

            if (_transitions.ContainsKey(trigger))
            {
                StateBase next = _transitions[trigger];

                return next;
            }

            switch (st.Result)
            {
                case StateExecutionResult.Working:
                    return st;

                case StateExecutionResult.Timeout:
                    return st;

                default:
                    _logger?.LogError($"Transition from state {st.Name} " +
                        $"on trigger {st.Result.ToString()} is not defined.");
                    return null;
            }
        }

        public int Count => _transitions.Count(); 
    }

    public class FSMExceptionTransitionNotDefined : Exception
    {
        public FSMExceptionTransitionNotDefined() : 
            base("State Transition is not defined") 
        { }
        
        public FSMExceptionTransitionNotDefined(string msg) : 
            base(msg) 
        { }
        public FSMExceptionTransitionNotDefined(StateBase st) : 
            base($"No transition from state {st.Name} " +
                $"on trigger {st.Result.ToString()} defined.") 
        { }
    }
}
