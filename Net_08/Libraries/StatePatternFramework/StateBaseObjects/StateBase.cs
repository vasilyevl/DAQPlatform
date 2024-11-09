using DAQFramework.Common;

using Grumpy.DaqFramework.Common;

using Microsoft.Extensions.Logging;

namespace Grumpy.StatePatternFramework
{
    public class IOResultToFsmStatusException: ArgumentException
    {
        public IOResultToFsmStatusException( string description, IOResults r) :
                        base(description)
        {
            IoResult = r;
        }

        public IOResults IoResult {
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

        private volatile StateExecutionResult _exequtionResult;

        private int _periodMs;
        private int _minPeriodMs;

        public StateBase(StateMachineBase? context, StateIDBase en,
            int period = InfiniteTimeout, ILogger? logger = null) {
            Context = context;
            LastError = string.Empty;

            PeriodMS = (period < InfiniteTimeout) ? InfiniteTimeout :
                (period < MinTimeoutLimitMs) ? MinTimeoutLimitMs : period;

            Result = StateExecutionResult.Working;
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

        public bool IsActive => Result == StateExecutionResult.Working;

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
        public StateExecutionResult Result {
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

        internal void ActivateState() => Result = StateExecutionResult.Working;

        public virtual void Enter() { }

        public virtual void Exit() { }

        abstract public void StateProc(StateProcArgs args);
    }
}
