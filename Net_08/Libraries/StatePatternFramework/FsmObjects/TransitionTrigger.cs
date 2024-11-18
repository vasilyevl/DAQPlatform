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

using System.Net.Http.Headers;

namespace Grumpy.StatePatternFramework
{
    #region Types:

    public delegate bool CustomTransitionCondition();

    public class TransitionTrigger : IEquatable<TransitionTrigger>
    {
        protected StateBase _currentState;
        protected StateResult _exitResult;
        protected CustomTransitionCondition? _customCondition;
        public TransitionTrigger(StateBase currentState, 
            StateResult result,
            CustomTransitionCondition? customCondition = null)
        {
            _currentState = currentState;
            _exitResult = result;
            _customCondition = customCondition;
        }

        public bool Equals(TransitionTrigger? other)=>  
                (other is not null)
                && (_currentState == other._currentState)
                && (_exitResult == other._exitResult)
                && (ReferenceEquals(_customCondition, other._customCondition));

        public override bool Equals(object? obj) => 
            (obj is not null) && Equals(obj as TransitionTrigger);

        public static bool operator == ( TransitionTrigger a, TransitionTrigger b)
        {
            return ((a is null) && (b is null)) ||
                ((a is not null) && a.Equals(b));
        }

        public static bool operator != ( TransitionTrigger a, TransitionTrigger b)
        {
            return !((a is null) && (b is null)) ||
                   ((a is not null) && (b is null)) ||
                   ((a is null) && (b is not null)) ||
                   !a.Equals(b);
        }

        public override int GetHashCode()
        {
            int hashcode = _currentState.Name.GetHashCode() + _exitResult.GetHashCode();
            return hashcode;
        }

        public StateBase CurrentState { get { return _currentState; } }
        public StateResult Status { get { return _exitResult; } }

        public override string ToString()
        {
            return $"Current state: {CurrentState.Name} / Status: {Status}.";
        }
    }
    #endregion // Types:
}
