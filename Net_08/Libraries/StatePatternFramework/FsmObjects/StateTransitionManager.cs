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
        protected ILogger? logger = null;

        private Dictionary<TransitionTrigger, StateBase> transitions;

        public StateTransitionManager(ILogger logger = null!) {
            this.logger = logger;
            transitions = [];
        }

        public bool LoggerIsSet => logger != null; 

        public bool AddTransition(StateBase initialState, 
                                  StateResult stateExitStatus, 
                                  StateBase nextState)
        {
            var transitionTrigger = 
                new TransitionTrigger(initialState, stateExitStatus);

            return AddTransition(transitionTrigger, nextState);
        }

        public bool AddTransition(TransitionTrigger transitionTrigger, 
                                  StateBase nextState)
        {

            if (!transitions.ContainsKey(transitionTrigger)) {

                transitions.Add(transitionTrigger, nextState);
                return true;
            }
            else {

                string msg = $"State Machine Transition Manager. " +
                    $"Attempt to add redundant transition to the state " +
                    $"{nextState.Name}:\nexecution result " +
                    $"{transitionTrigger.Status} to state " +
                    $"{transitions[transitionTrigger].Name}. " +
                    $"Request ignored.";

                logger?.LogWarning(msg);

                if (logger is null) {
                    throw new ExceptionTransitionNotDefined(msg);
                }

                return false;
            }
        }

        public bool ContainsTrigger(TransitionTrigger transitionTrigger) =>
            transitions.ContainsKey(transitionTrigger);
        

        public bool PeekNextState(TransitionTrigger trigger, 
            out StateBase? nextState)
        {
            nextState = (transitions?.ContainsKey(trigger) ?? false) ?
                transitions[trigger]:
                null;
  
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
            TransitionTrigger trigger = 
                new TransitionTrigger(st, st.Result);

            if (transitions.ContainsKey(trigger)) {
                return transitions[trigger];
            }

            switch (st.Result)
            {
                case StateResult.Working:
                case StateResult.Timeout:
                    return st;

                default:
                    string msg = $"State Machine Transition Manager. " +
                        $"No transition from state {st.Name} " +
                        $"on trigger {st.Result.ToString()} defined.";
                   
                    if(logger is null) {
                        throw new ExceptionTransitionNotDefined(msg);
                    }
                    else {
                        logger.LogWarning(msg);
                    }
                    return null;
            }
        }

        public int Count => transitions.Count(); 
    }

    public class ExceptionTransitionNotDefined : Exception
    {
        public ExceptionTransitionNotDefined() : 
            base("State Transition is not defined") 
        { }
        
        public ExceptionTransitionNotDefined(string msg) : 
            base(msg) 
        { }
        public ExceptionTransitionNotDefined(StateBase st) : 
            base($"No transition from state {st.Name} " +
                $"on trigger {st.Result.ToString()} defined.") 
        { }
    }
}
