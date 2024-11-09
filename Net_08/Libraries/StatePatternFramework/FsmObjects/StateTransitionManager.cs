/* 
Copyright (c) 2024 vasilyevl (Grumpy). Permission is hereby granted, 
free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"),to deal in the Software 
without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the 
Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

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
