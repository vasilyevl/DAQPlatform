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

    /// <summary>
    /// Delegate for custom transition conditions.
    /// </summary>
    public delegate bool CustomTransitionCondition();

    /// <summary>
    /// Represents a trigger for state transitions in the state machine framework.
    /// </summary>
    public class TransitionTrigger : IEquatable<TransitionTrigger>
    {
        protected StateBase _currentState;
        protected StateResult _exitResult;
        protected CustomTransitionCondition? _customCondition;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionTrigger"/> class.
        /// </summary>
        /// <param name="currentState">The current state.</param>
        /// <param name="result">The result that triggers the transition.</param>
        /// <param name="customCondition">The custom condition for the transition.</param>
        public TransitionTrigger(StateBase currentState,
            StateResult result,
            CustomTransitionCondition? customCondition = null) {
            _currentState = currentState;
            _exitResult = result;
            _customCondition = customCondition;
        }

        /// <summary>
        /// Determines whether the specified <see cref="TransitionTrigger"/> is equal to the current <see cref="TransitionTrigger"/>.
        /// </summary>
        /// <param name="other">The <see cref="TransitionTrigger"/> to compare with the current <see cref="TransitionTrigger"/>.</param>
        /// <returns>true if the specified <see cref="TransitionTrigger"/> is equal to the current <see cref="TransitionTrigger"/>; otherwise, false.</returns>
        public bool Equals(TransitionTrigger? other) =>
                (other is not null)
                && (_currentState == other._currentState)
                && (_exitResult == other._exitResult)
                && (ReferenceEquals(_customCondition, other._customCondition));

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="TransitionTrigger"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current <see cref="TransitionTrigger"/>.</param>
        /// <returns>true if the specified object is equal to the current <see cref="TransitionTrigger"/>; otherwise, false.</returns>
        public override bool Equals(object? obj) =>
            (obj is not null) && Equals(obj as TransitionTrigger);

        /// <summary>
        /// Determines whether two specified <see cref="TransitionTrigger"/> objects are equal.
        /// </summary>
        /// <param name="a">The first <see cref="TransitionTrigger"/> to compare.</param>
        /// <param name="b">The second <see cref="TransitionTrigger"/> to compare.</param>
        /// <returns>true if the two <see cref="TransitionTrigger"/> objects are equal; otherwise, false.</returns>
        public static bool operator ==(TransitionTrigger a, TransitionTrigger b) {
            return ((a is null) && (b is null)) ||
                ((a is not null) && a.Equals(b));
        }

        /// <summary>
        /// Determines whether two specified <see cref="TransitionTrigger"/> objects are not equal.
        /// </summary>
        /// <param name="a">The first <see cref="TransitionTrigger"/> to compare.</param>
        /// <param name="b">The second <see cref="TransitionTrigger"/> to compare.</param>
        /// <returns>true if the two <see cref="TransitionTrigger"/> objects are not equal; otherwise, false.</returns>
        public static bool operator !=(TransitionTrigger a, TransitionTrigger b) {
            return !((a is null) && (b is null)) ||
                   ((a is not null) && (b is null)) ||
                   ((a is null) && (b is not null)) ||
                   !a.Equals(b);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current <see cref="TransitionTrigger"/>.</returns>
        public override int GetHashCode() {
            int hashcode = _currentState.Name.GetHashCode() + _exitResult.GetHashCode();
            return hashcode;
        }

        /// <summary>
        /// Gets the current state.
        /// </summary>
        public StateBase CurrentState { get { return _currentState; } }

        /// <summary>
        /// Gets the status that triggers the transition.
        /// </summary>
        public StateResult Status { get { return _exitResult; } }

        /// <summary>
        /// Returns a string that represents the current <see cref="TransitionTrigger"/>.
        /// </summary>
        /// <returns>A string that represents the current <see cref="TransitionTrigger"/>.</returns>
        public override string ToString() {
            return $"Current state: {CurrentState.Name} / Status: {Status}.";
        }
    }
    #endregion // Types:
}
