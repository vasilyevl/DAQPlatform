namespace Grumpy.StatePatternFramework
{
    #region Types:

    public class TransitionTrigger : IEquatable<TransitionTrigger>
    {
        protected StateBase _currentState;
        protected StateExecutionResult _exitStatus;

        public TransitionTrigger(StateBase currentState, StateExecutionResult exitStatus)
        {
            _currentState = currentState;
            _exitStatus = exitStatus;
        }

        public bool Equals(TransitionTrigger? other)
        {
            if (other is null)
                return false;
            return (_currentState == other._currentState) && (_exitStatus == other._exitStatus);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            TransitionTrigger? tr = obj as TransitionTrigger;

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
        public StateExecutionResult Status { get { return _exitStatus; } }

        public override string ToString()
        {
            return $"Current state: {CurrentState.Name} / Status: {Status}.";
        }
    }
    #endregion // Types:
}
