using System;

namespace FSM
{
    #region Types:
    /// <summary> Device mode enumerator.
    /// </summary>

    public class TransitionTrigger : IEquatable<TransitionTrigger>
    {
        protected StateBase _currentState;
        protected FsmStateStatus _exitStatus;

        public TransitionTrigger(StateBase currentState, FsmStateStatus exitStatus)
        {
            _currentState = currentState;
            _exitStatus = exitStatus;
        }


        public bool Equals(TransitionTrigger other)
        {
            if (other is null)
                return false;
            return (_currentState == other._currentState) && (_exitStatus == other._exitStatus);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            TransitionTrigger tr = obj as TransitionTrigger;

            if (tr is null)
                return false;

            return ((this._currentState == tr._currentState) && (this._exitStatus == tr._exitStatus));
        }

        public static bool operator == ( TransitionTrigger a, TransitionTrigger b)
        {
            if (a is null)
                return false;

            return a.Equals(b);
        }

        public static bool operator != ( TransitionTrigger a, TransitionTrigger b)
        {
            if (a is null)
                return true;

            return (!a.Equals(b));
        }


        public override int GetHashCode()
        {
            int hashcode = _currentState.Name.GetHashCode() + _exitStatus.GetHashCode();
            return hashcode;
        }

        public StateBase CurrentState { get { return _currentState; } }
        public FsmStateStatus Status { get { return _exitStatus; } }

        public override string ToString()
        {
            return $"Current state: {CurrentState.Name} / Status: {Status}.";
        }
    }


    
    #endregion // Types:
}
