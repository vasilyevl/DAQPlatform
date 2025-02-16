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

using Grumpy.DAQFramework.Common;
using Microsoft.Extensions.Logging;

namespace Grumpy.StatePatternFramework
{
    /// <summary>
    /// Exception thrown when an IO result cannot be converted to a FSM status.
    /// </summary>
    public class IOResultToFsmStatusException : ArgumentException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IOResultToFsmStatusException"/> class.
        /// </summary>
        /// <param name="description">The description of the exception.</param>
        /// <param name="r">The IO result that caused the exception.</param>
        public IOResultToFsmStatusException(string description, Results r) : base(description) {
            IoResult = r;
        }

        /// <summary>
        /// Gets the IO result that caused the exception.
        /// </summary>
        public Results IoResult { get; private set; }
    }

    /// <summary>
    /// Base class for states in the state machine framework.
    /// </summary>
    public abstract class StateBase : IEquatable<StateBase>
    {
        public const int InfiniteTimeout = -1;
        public const int DefaultBaseTimeoutMs = 33;
        public const int MinTimeoutLimitMs = 15;

        private EnumBase _id;
        protected static ILogger? _logger = null;
        protected object _contextLock;
        private volatile StateResult _exequtionResult;
        private int _periodMs;
        private int _minPeriodMs;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateBase"/> class.
        /// </summary>
        /// <param name="context">The state machine context.</param>
        /// <param name="en">The state ID.</param>
        /// <param name="period">The period in milliseconds.</param>
        /// <param name="logger">The logger instance.</param>
        public StateBase(StateMachineBase? context, StateIDBase en, int period = InfiniteTimeout, ILogger? logger = null) {
            Context = context;
            LastError = string.Empty;

            PeriodMS = (period < InfiniteTimeout) ? InfiniteTimeout :
                (period < MinTimeoutLimitMs) ? MinTimeoutLimitMs : period;

            Result = StateResult.Working;
            _contextLock = new object();
            _id = en;

            if (logger is not null && _logger is null) {
                _logger = logger;
            }
        }

        /// <summary>
        /// Clears the last error.
        /// </summary>
        public void ClearError() => LastError = null;

        private string? _lastError;
        /// <summary>
        /// Gets or sets the last error.
        /// </summary>
        public string? LastError {
            get => _lastError;
            protected set => _lastError = value;
        }

        /// <summary>
        /// Gets the state machine context.
        /// </summary>
        public StateMachineBase? Context { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the logger is set.
        /// </summary>
        public bool LoggerIsSet => _logger is not null;

        /// <summary>
        /// Gets the name of the state.
        /// </summary>
        public string Name => ID.Name;

        /// <summary>
        /// Gets the state ID.
        /// </summary>
        public StateIDBase ID => new StateIDBase(_id.Name, _id.Id);

        /// <summary>
        /// Gets a value indicating whether the timeout is infinite.
        /// </summary>
        public bool TimeoutIsInfinite => PeriodMS <= InfiniteTimeout;

        /// <summary>
        /// Gets a value indicating whether the state is in continuous mode.
        /// </summary>
        public bool ContineousMode => PeriodMS == 0;

        /// <summary>
        /// Gets a value indicating whether the state uses a watchdog.
        /// </summary>
        public bool UsesWatchDog => !ContineousMode && !TimeoutIsInfinite;

        /// <summary>
        /// Gets a value indicating whether the state uses idling.
        /// </summary>
        public bool UsesIdling => UsesWatchDog || TimeoutIsInfinite;

        /// <summary>
        /// Sets the timeout to infinite.
        /// </summary>
        public void SetInfiniteTimeout() => PeriodMS = InfiniteTimeout;

        /// <summary>
        /// Gets or sets the period in milliseconds.
        /// </summary>
        public int PeriodMS {
            get => _periodMs;
            set => _periodMs = (value <= InfiniteTimeout) ?
                                    InfiniteTimeout :
                                    Math.Max(value, MinPeriodMs);
        }

        /// <summary>
        /// Gets or sets the minimum period in milliseconds.
        /// </summary>
        public int MinPeriodMs {
            get => _minPeriodMs;
            set => _minPeriodMs = (value <= InfiniteTimeout) ?
                                    MinTimeoutLimitMs :
                                    Math.Max(value, MinTimeoutLimitMs);
        }

        /// <summary>
        /// Gets a value indicating whether the state is active.
        /// </summary>
        public bool IsActive => Result == StateResult.Working;

        /// <summary>
        /// Determines whether the specified state is equal to the current state.
        /// </summary>
        /// <param name="other">The state to compare with the current state.</param>
        /// <returns>true if the specified state is equal to the current state; otherwise, false.</returns>
        public bool Equals(StateBase? other) {
            if (other is null)
                return false;
            return (Name == other.Name)
                    && (ID == other.ID)
                    && (this.GetType() == other.GetType());
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current state.
        /// </summary>
        /// <param name="other">The object to compare with the current state.</param>
        /// <returns>true if the specified object is equal to the current state; otherwise, false.</returns>
        public override bool Equals(object? other) {
            if (other is null)
                return false;

            StateBase? st = other as StateBase;

            if (st is null)
                return false;

            return Equals(st);
        }

        /// <summary>
        /// Determines whether two specified states are equal.
        /// </summary>
        /// <param name="a">The first state to compare.</param>
        /// <param name="b">The second state to compare.</param>
        /// <returns>true if the two states are equal; otherwise, false.</returns>
        public static bool operator ==(StateBase a, StateBase b) {
            if (a is null)
                return b is null ? true : false;

            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two specified states are not equal.
        /// </summary>
        /// <param name="a">The first state to compare.</param>
        /// <param name="b">The second state to compare.</param>
        /// <returns>true if the two states are not equal; otherwise, false.</returns>
        public static bool operator !=(StateBase a, StateBase b) {
            if (a is null)
                return b is null ? false : true;

            return (!a.Equals(b));
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current state.</returns>
        public override int GetHashCode() {
            return base.GetHashCode();
        }

        private object _stateResultLock = new object();
        /// <summary>
        /// Gets or sets the result of the state execution.
        /// </summary>
        public StateResult Result {
            get {
                lock (_stateResultLock) {
                    return _exequtionResult;
                }
            }
            protected set {
                lock (_stateResultLock) {
                    _exequtionResult = value;
                }
            }
        }

        /// <summary>
        /// Activates the state.
        /// </summary>
        internal void ActivateState() => Result = StateResult.Working;

        /// <summary>
        /// Called when the state is entered.
        /// </summary>
        public virtual void Enter() { }

        /// <summary>
        /// Called when the state is exited.
        /// </summary>
        public virtual void Exit() { }

        /// <summary>
        /// Processes the state logic.
        /// </summary>
        /// <param name="args">The state process arguments.</param>
        abstract public void StateProc(StateProcArgs args);
    }
}
