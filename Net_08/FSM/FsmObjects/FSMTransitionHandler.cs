using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;

namespace FSM
{
    public class FSMTransitionHandler 
    {
        protected static ILogger _logger = Log.Logger;

        private Dictionary<TransitionTrigger, StateBase> _transitionTable = 
                    new Dictionary<TransitionTrigger, StateBase>();

        public bool AddTransition(StateBase initialState, FsmStateStatus stateExitStatus, StateBase nextState)
        {
            var transitionTrigger = new TransitionTrigger(initialState, stateExitStatus);

            return AddTransition(transitionTrigger, nextState);
        }

        public bool AddTransition(TransitionTrigger transitionTrigger, StateBase nextState)
        {

            if (!_transitionTable.ContainsKey(transitionTrigger))
            {
                _transitionTable.Add(transitionTrigger, nextState);
                return true;
            }

           _logger.Error($"FSM Transition table already contains transition defined for state {transitionTrigger.CurrentState.Name} " +
                $"exit code {transitionTrigger.Status} to state {_transitionTable[transitionTrigger].Name}. " +
                $"Cant add another transition to state {nextState.Name} with the same trigger. Request ignored.");

            return false;
        }

        public bool ContainsTrigger(TransitionTrigger transitionTrigger)
        {
            return _transitionTable.ContainsKey(transitionTrigger);
        }

        public bool PeekNextState(TransitionTrigger trigger, out StateBase nextState)
        {       
            if (_transitionTable.ContainsKey(trigger)) {
                nextState = _transitionTable[trigger];
            }
            else {
                nextState = null;
            }
            return nextState != null;
        }

        public bool PeekNextStateName(TransitionTrigger trigger, out string nextStateName)
        {            
            if ( PeekNextState(trigger, out StateBase nextState)) {
                nextStateName = nextState.Name;
            }
            else {
                nextStateName = null;
            }

            return nextStateName != null;
        }

        public StateBase NextState(StateBase st)
        {
            TransitionTrigger trigger = new TransitionTrigger(st, st.StateStatus);

            if (_transitionTable.ContainsKey(trigger))
            {
                StateBase next = _transitionTable[trigger];
               // Console.WriteLine($"State selected: {next.Name }.");
                return next;
            }

            // Check if default transition can be used. 
            switch (st.StateStatus)
            {
                case FsmStateStatus.Active:
                    return st;

                case FsmStateStatus.TimeOut:
                    return st;

                default:
                    string msg = $"No transition from state {st.Name} on trigger {st.StateStatus.ToString()} defined.";
                   _logger.Error(msg);
                    throw new FSMExceptionTransitionNotDefined(msg);       
            }
        }

        public int Count
        {
            get { return _transitionTable.Count(); }
        }
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
                $"on trigger {st.StateStatus.ToString()} defined.") 
        { }

    }
}
