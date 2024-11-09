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
