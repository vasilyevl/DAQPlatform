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

using System.Numerics;
using System.Transactions;

namespace Grumpy.StatePatternFramework
{
    [Flags]
    public enum StateResult: uint
    {
        NA = 0,
        Success = 1,
        Error = 2,
        Timeout = 4,
        Idling = 8,
        Working = 16,
        Aborted = 32,

        IdlingAborted = Idling | Aborted,
        WorkingAborted = Working | Aborted,
        ErrorAborted = Error | Aborted,
        Complete = Success | Error | Timeout | Aborted
    }

    // <summary>
    /// Represents the result of a state operation.
    /// </summary>
    class StateOperationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateOperationResult"/> class with the specified result.
        /// </summary>
        /// <param name="result">The result of the state operation.</param>
        public StateOperationResult(StateResult result) {
            Result = result;
        }

        /// <summary>
        /// Gets the result of the state operation.
        /// </summary>
        public StateResult Result { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the state operation was aborted.
        /// </summary>
        public bool Aborted => (Result & StateResult.Aborted) != 0;

        /// <summary>
        /// Gets a value indicating whether the state operation was completed.
        /// </summary>
        public bool Completed => (Result & StateResult.Success) != 0;

        /// <summary>
        /// Gets a value indicating whether the state operation encountered an error.
        /// </summary>
        public bool Error => (Result & StateResult.Error) != 0;

        /// <summary>
        /// Gets a value indicating whether the state operation is idling.
        /// </summary>
        public bool Idling => (Result & StateResult.Idling) != 0;

        /// <summary>
        /// Gets a value indicating whether the state operation is working.
        /// </summary>
        public bool Working => (Result & StateResult.Working) != 0;

        /// <summary>
        /// Gets a value indicating whether the state operation is not available.
        /// </summary>
        public bool NotAvailable => (Result & StateResult.NA) != 0;

        /// <summary>
        /// Gets the string representation of the state result.
        /// </summary>
        /// <param name="result">The state result.</param>
        /// <returns>The string representation of the state result.</returns>
        public static string GetStateResultString(StateResult result) {
            return result switch {
                StateResult.Success => "Completed",
                StateResult.Error => "Error",
                StateResult.Timeout => "Timeout",
                StateResult.Idling => "Idling",
                StateResult.Working => "Working",
                StateResult.NA => "NotAvailable",
                StateResult.Aborted => "Aborted",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Determines whether two <see cref="StateOperationResult"/> instances are equal.
        /// </summary>
        /// <param name="left">The first <see cref="StateOperationResult"/> to compare.</param>
        /// <param name="right">The second <see cref="StateOperationResult"/> to compare.</param>
        /// <returns>true if the specified <see cref="StateOperationResult"/> instances are equal; otherwise, false.</returns>
        public static bool operator ==(StateOperationResult left, StateOperationResult right) {
            if (ReferenceEquals(left, right)) {
                return true;
            }

            if (left is null || right is null) {
                return false;
            }

            return left.Result == right.Result;
        }

        /// <summary>
        /// Determines whether two <see cref="StateOperationResult"/> instances are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="StateOperationResult"/> to compare.</param>
        /// <param name="right">The second <see cref="StateOperationResult"/> to compare.</param>
        /// <returns>true if the specified <see cref="StateOperationResult"/> instances are not equal; otherwise, false.</returns>
        public static bool operator !=(StateOperationResult left, StateOperationResult right) {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object? obj) {
            if (obj is StateOperationResult other) {
                return this == other;
            }

            return false;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() {
            return Result.GetHashCode();
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="StateOperationResult"/> to a <see cref="StateResult"/>.
        /// </summary>
        /// <param name="stateOperationResult">The <see cref="StateOperationResult"/> to convert.</param>
        public static implicit operator StateResult(StateOperationResult stateOperationResult) {
            return stateOperationResult.Result;
        }

        /// <summary>
        /// Defines an explicit conversion of a <see cref="StateResult"/> to a <see cref="StateOperationResult"/>.
        /// </summary>
        /// <param name="stateResult">The <see cref="StateResult"/> to convert.</param>
        public static explicit operator StateOperationResult(StateResult stateResult) {
            return new StateOperationResult(stateResult);
        }

        /// <summary>
        /// Defines a bitwise OR operator for two <see cref="StateOperationResult"/> instances.
        /// </summary>
        /// <param name="left">The first <see cref="StateOperationResult"/> to compare.</param>
        /// <param name="right">The second <see cref="StateOperationResult"/> to compare.</param>
        /// <returns>A new <see cref="StateOperationResult"/> that is the result of the bitwise OR operation.</returns>
        public static StateOperationResult operator |(StateOperationResult left, StateOperationResult right)
        {
            return new StateOperationResult(left.Result | right.Result);
        }

        /// <summary>
        /// Defines a bitwise AND operator for two <see cref="StateOperationResult"/> instances.
        /// </summary>
        /// <param name="left">The first <see cref="StateOperationResult"/> to compare.</param>
        /// <param name="right">The second <see cref="StateOperationResult"/> to compare.</param>
        /// <returns>A new <see cref="StateOperationResult"/> that is the result of the bitwise AND operation.</returns>
        public static StateOperationResult operator &(StateOperationResult left, StateOperationResult right)
        {
            return new StateOperationResult(left.Result & right.Result);
        }

        /// <summary>
        /// Defines a bitwise OR operator for a <see cref="StateOperationResult"/> and a <see cref="StateResult"/>.
        /// </summary>
        /// <param name="left">The <see cref="StateOperationResult"/> to compare.</param>
        /// <param name="right">The <see cref="StateResult"/> to compare.</param>
        /// <returns>A new <see cref="StateOperationResult"/> that is the result of the bitwise OR operation.</returns>
        public static uint operator |(StateOperationResult left, StateResult right) {
            return (uint)(left.Result | right);
        }

        /// <summary>
        /// Defines a bitwise AND operator for a <see cref="StateOperationResult"/> and a <see cref="StateResult"/>.
        /// </summary>
        /// <param name="left">The <see cref="StateOperationResult"/> to compare.</param>
        /// <param name="right">The <see cref="StateResult"/> to compare.</param>
        /// <returns>A new <see cref="StateOperationResult"/> that is the result of the bitwise AND operation.</returns>
        public static uint operator &(StateOperationResult left, StateResult right) {
            return (uint) (left.Result & right);
        }
    }
}
