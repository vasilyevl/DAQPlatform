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
    public class IOResultToFsmStatusException: ArgumentException
    {
        public IOResultToFsmStatusException( 
            string description, Results r) : base(description)
        {
            IoResult = r;
        }

        public Results IoResult {
            get;
            private set;
        }
    }

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

        public StateBase(StateMachineBase? context, StateIDBase en,
            int period = InfiniteTimeout, ILogger? logger = null) {
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
        public void ClearError() => LastError = null;

        private string? _lastError;
        public string? LastError {
            get => _lastError;
            protected set => _lastError = value;
        }


        public StateMachineBase? Context {
            get;
            private set;
        }

        public bool LoggerIsSet => _logger is not null;

        public string Name => ID.Name;

        public StateIDBase ID => new StateIDBase(_id.Name, _id.Id);

        public bool TimeoutIsInfinite => PeriodMS <= InfiniteTimeout;

        public bool ContineousMode => PeriodMS == 0;

        public bool UsesWatchDog => !ContineousMode && !TimeoutIsInfinite;

        public bool UsesIdling => UsesWatchDog || TimeoutIsInfinite;
        public void SetInfiniteTimeout() => PeriodMS = InfiniteTimeout;

        public int PeriodMS {
            get => _periodMs;
            set => _periodMs = (value <= InfiniteTimeout) ?
                                    InfiniteTimeout :
                                    Math.Max(value, MinPeriodMs);
        }

        public int MinPeriodMs {
            get => _minPeriodMs;
            set => _minPeriodMs = (value <= InfiniteTimeout) ?
                                    MinTimeoutLimitMs :
                                    Math.Max(value, MinTimeoutLimitMs);
        }

        public bool IsActive => Result == StateResult.Working;

        public bool Equals(StateBase? other) {
            if (other is null)
                return false;
            return (Name == other.Name)
                    && (ID == other.ID)
                    && (this.GetType() == other.GetType());
        }

        override public bool Equals(object? other) {
            if (other is null) {

                return false;
            }

            StateBase? st = other as StateBase;

            if (st is null) {
                return false;
            }

            return Equals(st);
        }

        public static bool operator ==(StateBase a, StateBase b) {
            if (a is null) {

                return b is null ? true : false;

            }

            return a.Equals(b);
        }

        public static bool operator !=(StateBase a, StateBase b) {
            if (a is null)
                return b is null ? false : true;

            return (!a.Equals(b));
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        private object _stateResultLock = new object();
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

        internal void ActivateState() => Result = StateResult.Working;

        public virtual void Enter() { }

        public virtual void Exit() { }

        abstract public void StateProc(StateProcArgs args);
    }
}
