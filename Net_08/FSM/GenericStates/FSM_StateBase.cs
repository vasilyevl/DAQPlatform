using Serilog;

using System;
using System.Collections.Generic;

using Tau.Common;
using Tau.ControlBase;
using Tau.HwControlBase;

namespace FSM
{
    public class IOResultToFsmStatusException: ArgumentException
    {
        public IOResultToFsmStatusException( string description, IOState r) : base(description)
        {
            IoResult = r;
        }

        public IOState IoResult {
            get;
            private set;
        }
    }


    public class StateEnumBase :EnumeratorBase
    {
        public static StateEnumBase NA = new StateEnumBase(nameof(NA), 0);
        public static StateEnumBase Start = new StateEnumBase(nameof(Start), 1);
        public static StateEnumBase Stop = new StateEnumBase( nameof(Stop), 2);
        public static StateEnumBase End = new StateEnumBase(nameof(End), 3);
        public static StateEnumBase GenericError = new StateEnumBase(nameof(GenericError), 4);
        public StateEnumBase(string name, int id ) : base( name, id) {
        }

    }
    public abstract class StateBase : IEquatable<StateBase>
    {

        private static readonly Dictionary<IOState, FsmStateStatus> _ioResultToStateStatus = new Dictionary<IOState, FsmStateStatus>()
        {
            { IOState.Success, FsmStateStatus.Complete },
            { IOState.Error, FsmStateStatus.Error },
            { IOState.Cancelled, FsmStateStatus.Complete },
            { IOState.Warning, FsmStateStatus.Complete }        
        };

        private static readonly Dictionary<CommandState, FsmStateStatus> _commandStatusToStateStatus = new Dictionary<CommandState, FsmStateStatus>()
        {
            {CommandState.Success, FsmStateStatus.Complete},
            {CommandState.Ignored, FsmStateStatus.Complete},
            {CommandState.Rejected, FsmStateStatus.Complete},
            {CommandState.Failed, FsmStateStatus.Error},
            {CommandState.Timeout, FsmStateStatus.Error},
        };

        public static FsmStateStatus FsmStateStatusFromIOResult( IOState r, bool throwException = true)
        {
            if (_ioResultToStateStatus.ContainsKey(r)) {

                return _ioResultToStateStatus[r];
            }

            if (throwException) {

                throw new IOResultToFsmStatusException($"Unsupported IOResalt value.", r);
            }

            return FsmStateStatus.NA;

        }

        public static FsmStateStatus FsmStateStatusFromCommandStatus(CommandState st, bool throwException = true)
        {
            if (_commandStatusToStateStatus.ContainsKey(st)) {

                return _commandStatusToStateStatus[st];
            }

            if (throwException) {

                throw new ArgumentException($"Unsupported IOResalt value: {st}");
            }

            return FsmStateStatus.NA;

        }

        public const int InfiniteTimeout = -1;
        public const int DefaultBaseTimeoutMs = 33;
        public const int MinTimeoutLimitMs = 15;

        private EnumeratorBase _id;
        protected static ILogger _logger = Log.Logger;

        protected object _contextLock;

        private volatile FsmStateStatus _status;

        private int _periodMs;
        private int _minPeriodMs;


        public StateBase(StateEnumBase en, int period = InfiniteTimeout)
        {
            PeriodMS = (period < InfiniteTimeout) ? InfiniteTimeout :
                (period < MinTimeoutLimitMs) ? MinTimeoutLimitMs : period;

            StateStatus = FsmStateStatus.Active;
            _contextLock = new object();
            _id = en;
        }

        public string Name => ID.Name;

        public EnumeratorBase ID {
            get { 
                return  new StateEnumBase(_id.Name, _id.Id) as EnumeratorBase; 
            }
        }

        public bool TimeoutIsInfinite => PeriodMS <= InfiniteTimeout;

        public bool ContineousMode => PeriodMS == 0 ;

        public bool UsesWatchDog => ! ContineousMode && !TimeoutIsInfinite;

        public bool UsesIdling => UsesWatchDog || TimeoutIsInfinite;
        public void SetInfiniteTimeout() => PeriodMS = InfiniteTimeout;

        public int PeriodMS
        {
            get =>  _periodMs; 
            set => _periodMs = ( value <= InfiniteTimeout) ?  InfiniteTimeout :  Math.Max(value, MinPeriodMs); 
        }

        public int MinPeriodMs
        {
            get => _minPeriodMs;
            set => _minPeriodMs = (value <= InfiniteTimeout) ? MinTimeoutLimitMs : Math.Max(value, MinTimeoutLimitMs);
        }

        public bool IsActive => StateStatus == FsmStateStatus.Active;

        public bool Equals(StateBase other)
        {
            if (other is  null)
                return false;
            return  (Name == other.Name) && (ID == other.ID) && (this.GetType() == other.GetType());
        }

        override public bool Equals(object other)
        {
            if (other is null) {
             
                return false;
            }

            StateBase st = other as StateBase;

            if (st is null) {
                return false;
            }

            return Equals(st);
        }

        public static bool operator == ( StateBase a, StateBase b)
        {
            if (a is null) {
             
                return b is null? true: false;

            }

            return a.Equals(b);
        }

        public static bool operator != (StateBase a, StateBase b)
        {
            if (a is null)
                return b is null ? false : true;

            return (!a.Equals(b));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public FsmStateStatus StateStatus
        {
            get 
            {
                FsmStateStatus result;
                System.Threading.Thread.MemoryBarrier();
                result = _status;
                System.Threading.Thread.MemoryBarrier();
                return result;
            }
            protected set {

                System.Threading.Thread.MemoryBarrier();
                _status = value;
                System.Threading.Thread.MemoryBarrier();

            }
        }


        internal void ActivateState() => StateStatus = FsmStateStatus.Active;

        public virtual void Enter(){}

        public virtual void Exit(){}
        
        abstract public void StateProc(StateProcArgs args );
    }
}
