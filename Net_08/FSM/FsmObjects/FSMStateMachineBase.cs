using Tau.ControlBase;

using Serilog;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Tau.Common;

namespace FSM
{
    /// <summary> Base class for Tau FSM device
    /// 
    /// 
    /// </summary>

    [Flags]
    public enum FsmEngineIdlingResult
    {
        NA = 0,
        IdlingInterrupted = 1,
        CommandPending = 2,
        Tick = 4,
        Timeout = 8,
        ContineousRun = 16
    }

    public class StateChangeEventArgs : EventArgs {
        private EnumeratorBase _newState;
        private EnumeratorBase _previousState;

        public StateChangeEventArgs( EnumeratorBase newState, EnumeratorBase previousState = null )
        {
            _previousState = previousState;
            _newState = newState;
        }

        public  EnumeratorBase PreviousState => _previousState;
        public EnumeratorBase NewState => _newState;
    }

    public class StateProcArgs: EventArgs
    {
        public StateProcArgs(): base()
        {
            IdlingResult = FsmEngineIdlingResult.NA;
        }

        public StateProcArgs(FsmEngineIdlingResult st ) : base()
        {
            IdlingResult = st;
        }

        public FsmEngineIdlingResult IdlingResult { get; set; }
    }

    public delegate void StateChangeEventHandler(object sender, StateChangeEventArgs e);
    public class FSMBase
    {
        protected static readonly ILogger _logger = Log.Logger;

        protected const int EngineStartTimeout = 50;
        protected const int EngineStartPeriodMs = 1;
        protected const int DefaultWorkerTerminationTimeoutMs = 5000;
        protected const int _StateHistoryDepth = 32;

        #region Members

        protected ControlStack<StateBase> _statesHistory;
        protected FSMTransitionHandler _transitionHandler;
        // FSM thread.
        protected Thread _fsmWorkerThread;                 
        AutoResetEvent _fsmWorkerResetEvent;
        protected object _fsmWorkerThreadLock;

        private CancellationTokenSource _threadCts;
        protected CancellationToken _threadCt;

        private Timer _idlingTimer;
        // Maximum time it took to Execute any of the states in FSMPrdMs. 
        protected long _maxFsmExecTime;
        // Keeps number of times it took longer than _periodInTcks
        // to execute any state function.
        protected long _missedTicksCounter;
        // Time keeper. 
        protected long _engineTimeOutClock;             

        protected object _stateLock;

        private FsmEngineIdlingResult _lastIdlingResult;

        private object _idlingCallBackReentryLock;
        private bool _timerFired;

        private object _carrentStateLock;
        private StateBase _currentState;
        public StateQueue StateQueue { get; protected set; }
        #endregion Members

        public event StateChangeEventHandler StateChangeEvent;

        #region Constructors:
        /// <summary> Constructor with Name only  as a parameter. 
        ///  </summary>Such device will be considered independent or master.
        public FSMBase(string name, bool logTransitions = false)
        {
            Name = name;
            _fsmWorkerThread = null;
            _fsmWorkerThreadLock = new object();
       
            _maxFsmExecTime = 0;
            _missedTicksCounter = 0;
            _engineTimeOutClock = 0;

            _lastIdlingResult = FsmEngineIdlingResult.NA;
            _idlingCallBackReentryLock = new object();
            _timerFired = false;

            _threadCts = null;

            _fsmWorkerResetEvent = null;

            LogTransitions = logTransitions;
            StateQueue = new StateQueue();
            _InitFSM();
            // This virtual method adds custom states to the dictionary
            // and defines transitions.
            try {
                InitFSM();
            }
            catch (Exception ex) {
                string msg = $"FSM \"{Name}\". InitFSM() call failed. " +
                    $"Check Transition table." +
                    $"Exception: {ex.Message}";
                _logger.Error(msg);
                throw new Exception(msg);
            }
        }

        public string Name { get; set; }
        public bool InStateSequence => StateQueue.Count > 0;

        public StateBase CurrentState {
            get {
                lock (_carrentStateLock) {
                    return _currentState;
                }
            }
            protected set {
                lock (_carrentStateLock) {
                    _currentState = value;
                }
            }
        }
        #endregion //Constructors:

        private void _InitFSM() { 
                        
            _stateLock = new object ();
            _carrentStateLock = new object ();
            _transitionHandler = new FSMTransitionHandler();

            States = new FSMStatesDictionary();
            // The first state always:
            AddState(new StartState(this));
            // These dummy states used FSM stopping.
            AddState(new EndState(this));
            AddState(new StopState(this));
            CurrentState = States[StateEnumBase.Start];
            _statesHistory = new ControlStack<StateBase>(_StateHistoryDepth);
        }

        virtual public void InitFSM()
        { }

        virtual public bool CommandPending => false;

        #region Public Properties:
        public FSMStatesDictionary States {
            get; private set;
        }

        public bool LogTransitions { get; set;  }

        /// <summary> True if FSM worker thread is alive 
        /// (not terminated or aborted).
        /// </summary>
        public bool FsmIsRunning => 
            _fsmWorkerThread?.IsAlive ?? false;  

        public bool FsmIsPaused => 
            (_fsmWorkerThread != null) ? 
            _fsmWorkerThread.ThreadState == ThreadState.WaitSleepJoin : false;

        public bool FsmIsSuspended => 
            (_fsmWorkerThread != null) ? 
            _fsmWorkerThread.ThreadState == ThreadState.Suspended : false;

        /// <summary> Returns max state execution time since it has been 
        /// reset last time.</summary> Any attempt to change this parameter 
        /// simply resets it. 
        public long MaxExecTime {
            get => _maxFsmExecTime;
            // User can not set it, just to reset... .
            set => _maxFsmExecTime = 0;   
        }

        /// <summary> Returs number of missed Ticks sinse 
        /// last start or counter reset. 
        /// 
        /// </summary>
        public long NumberOfMissedTicks {
            get =>(_missedTicksCounter); 
            protected set => _missedTicksCounter = value;
        }

        public FSMTransitionHandler TransitionHandler => _transitionHandler;
        
        public bool MissedTicks => _missedTicksCounter > 0; 
        #endregion // Public Properties:

        #region Protected Methods       
        protected int AddState(StateBase state)
        {
            if (States.ContainsKey(state.ID)) {
                string err = $"FSM \"{Name}\". Failed to add state " +
                    $"{state.Name} to the list";
                _logger.Error(err);
                throw new FSMException(err);
            }

            States.Add(state);
            return States.Count;
        }

        protected int AddStatesFromList(List<StateBase> states)
        {
            int errCntr = 0;
            int statesAdded = 0;
            foreach (StateBase st in states) {
                try {
                    statesAdded = AddState(st);
                }
                catch (FSMException fsmEx) {
                    _logger.Error($"FSM \"{Name}\" : " +
                        $"Exception: {fsmEx.Message}");
                    errCntr++;
                }
            }
            return statesAdded;
        }

        protected virtual void RaiseStateChangeEvent( EnumeratorBase newState, EnumeratorBase previousState)
        {
            if (StateChangeEvent != null) {

                Delegate[] invocationList = StateChangeEvent.GetInvocationList();

                if (States[newState] != null) {

                    if (StateChangeEvent != null) {

                        Task.Factory.StartNew(() =>
                        {
                            StateChangeEvent.Invoke( this, 

                                new StateChangeEventArgs(newState, previousState));
                        }, TaskCreationOptions.LongRunning);
                    }
                }
                else {
                    // "Stop" is a special state, which FSM machine might not need to switch to.
                    // It can simply stop FSM engine when switch to "Stop" requested.
                    if (!newState.Name.Equals("Stop", StringComparison.OrdinalIgnoreCase)) {

                        throw new Exception($"FSM \"{Name}\". " +
                            $"RaiseStateChangeEvent(). State {newState} " +
                            $"not registered.");
                    }
                }
            }
        }

        #endregion Protected Methods


        #region Public Methods:

        public bool GetState(string name, ref StateBase st)
        {
            st = States[name];
            return st != null;
        }

        public ControlStack<StateBase> HistoryStack => _statesHistory;
        public List<StateBase> StateHistory => 
            _statesHistory?.PeekAllAsList() ?? new List<StateBase>();
        public StateBase PreviousState => 
            _statesHistory?.Peek(out StateBase last) ?? false? last : null;

        private void _IntitNewState(StateBase nextState)
        {
            if (nextState == null) {
                throw new NullReferenceException($"New state is not selectcted. " +
                    $"FSM \"{Name}\", Current state {CurrentState.Name}");
            }
            
            _idlingTimer?.Dispose();

            HistoryStack?.Push(CurrentState, force: true);
            
            
            CurrentState = nextState;
            CurrentState.ActivateState();
            CurrentState.Enter();

            lock (_fsmWorkerThreadLock) {

                if (CurrentState.TimeoutIsInfinite || 
                    CurrentState.UsesWatchDog) {

                    _fsmWorkerResetEvent = 
                        new AutoResetEvent(initialState: false);
                }
                else {
                    _fsmWorkerResetEvent.Close();
                    _fsmWorkerResetEvent = null;
                }
            }

            if (CurrentState.UsesWatchDog) {
                _idlingTimer = 
                    new System.Threading.Timer(_IdlingWakeUpCallBack, 
                        null, CurrentState.PeriodMS, CurrentState.PeriodMS);
            }
        }


        public void _IdlingWakeUpCallBack(object info)
        {
            bool lockTaken = false;
            try {
                Monitor.TryEnter(_idlingCallBackReentryLock, 0, ref lockTaken);

                if (lockTaken) {

                    lock (_fsmWorkerThreadLock) {
                        // Resume thread if paused.
                        if (FsmIsRunning && FsmIsPaused) {
                            _timerFired = true;
                            _fsmWorkerResetEvent?.Set();
                        }
                    }
                }
                else {
                    _missedTicksCounter++;
                    _logger.Debug($" FSM \"{Name}\": Watchdog timer " +
                        $"failed to enter callback.");
                }
            }
            catch(Exception e) {
                _missedTicksCounter++;
                _logger.Warning($"FSM \"{Name}\". _IdlingWakeUp() exception: {e.Message}");
            }
            finally {
                if (lockTaken) {
                    Monitor.Exit(_idlingCallBackReentryLock);
                }
            }
        }

        private bool _KeepGoing() => !_threadCt.IsCancellationRequested &&
                                     CurrentState != null &&
                                     CurrentState.Name != "Stop" &&
                                     CurrentState.Name != "End";

        ///<summary> That is main FSM worker function.
        /// It runs the FSM and shall be started by StartFSMEngine 
        /// in a separate thread if this object is not a slave.
        ///</summary>
        ///
        protected virtual void _FSMEngine()
        {
            //_logger.Debug($"{Name} device FSM engine starts.");

            // Reset timeout marker.
            _engineTimeOutClock = DateTime.Now.Ticks;

            if (CurrentState == null) {

                _logger.Error($"FSM \"{Name}\" failed to start. " +
                    $"Intial state is not set.");
                return;
            }

            //Enter Imnitial state...  
            CurrentState.Enter();

            _logger.Information($"FSM \"{Name}\" engine started. " +
                $"{CurrentState.Name} initial state entered.");

            _lastIdlingResult = FsmEngineIdlingResult.NA;

            while (_KeepGoing()) { 

                while (CurrentState.IsActive) {

                    CurrentState.StateProc(new StateProcArgs(_lastIdlingResult));
                   
                    if (CurrentState.IsActive) {

                        _lastIdlingResult = 
                            CurrentState.UsesIdling ? 
                                _Idling() : FsmEngineIdlingResult.ContineousRun;
                    }

                    if (_threadCt.IsCancellationRequested) {
                        _logger.Warning($"FSM \"{Name}\" cancellation requested. " +
                            $"Exitting FSM thread worker.");
                        return;
                    }
                }

                _DisarmIdlingTimer();

                CurrentState.Exit();
                
                if (_SelectNextState(out StateBase nextState)) {

                    EnumeratorBase previousStateID = CurrentState.ID;
                    _IntitNewState(nextState);
                    RaiseStateChangeEvent(CurrentState.ID, previousStateID);
                }
                else {
                    // Error. Exit worker.
                    break;
                }
            }          
            _logger.Information($"Stopping {Name}  FSM Engine.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _DisarmIdlingTimer()
        {
            try {
                Monitor.Enter(_idlingCallBackReentryLock);

                _idlingTimer?.Dispose();
                _idlingTimer = null;
                _lastIdlingResult = FsmEngineIdlingResult.NA;
            }
            catch ( Exception ex_) {
                _logger.Warning($"FSM \"{Name}\". Exception while disarming " +
                    $"idling timer. Exception: {ex_.Message}");
            }
            finally {
                Monitor.Exit(_idlingCallBackReentryLock);
            }
        }

        private bool _SelectNextState(out StateBase nextState)
        {
            if (!CustomTransitionHandler(out nextState)) {

                try {
                    nextState = _transitionHandler.NextState(CurrentState);
                }
                catch {
                    _logger.Error($"FSM \"{Name}\". Failed to select next " +
                        $"state for {CurrentState.Name} " +
                        $"/ {CurrentState.StateStatus}");

                    if (States["TransitionError"] != null) {
                        nextState = States["TransitionError"];
                    }
                    else {
                        return false;
                    }
                }
            }

            if (LogTransitions) {

                _logger.Debug( $"FSM \"{Name}\" Engine. Switching from " +
                               $"state \"{(CurrentState?.Name ?? "None")}\" " +
                               $" to \"{nextState.Name}\"" );
            }

            return true;
        }
    
        virtual protected bool CustomTransitionHandler(out StateBase nextState)
        {
            if (StateQueue.Count > 0) {
                if (StateQueue.TryDequeue(out nextState)) {
                    return !(nextState is null);
                }
            }
            nextState = null;
            return false;
        }

        public int AddTransitions(
            List<Tuple<StateBase, FsmStateStatus, StateBase>> transitions)
        {
            foreach (var item in transitions) {

                AddTransition(item.Item1, item.Item2, item.Item3);
            }
            return _transitionHandler.Count;
        }

        public bool AddTransition ( string currentState, 
            FsmStateStatus status, string nextState)
        {
            string error = String.Empty;
            
            try {

                if (States[currentState] == null) {
                    error = $"FSM \"{Name}\". Can't add transition from state {currentState} " +
                        $"to {nextState} on {status}. " +
                        $"Current state {currentState} object does not exist.";
                    _logger.Error(error);
                    return false;
                }

                if (States[nextState] == null) {
                    error = $"FSM \"{Name}\". Can't add transition from state {currentState} " +
                        $"to {nextState} on {status}. " + 
                        $"Next state {nextState} object does not exist. ";
                    _logger.Error(error);
                    return false;
                }

                var trigger = new TransitionTrigger(States[currentState], status);

                if (!_transitionHandler.ContainsTrigger(trigger)) {

                    AddTransition(States[currentState], status, States[nextState]);
                    return true;

                }

                if (_transitionHandler.PeekNextStateName(trigger, 
                                         out string nextStateName)) {

                    if (nextStateName.Equals(nextState)) {
                        _logger.Warning($"FSM \"{Name}\". Attempt to add state " +
                            $"transition which is already added. " +
                            $"{trigger} / {nextState}. Request ignored.");
                        return true;
                    }
                }

                error += $"FSM \"{Name}\". Transition from current state " +
                    $"{ currentState} on trigger " +
                    $"\"{currentState} / {status}\" already added.\n" +
                    $"Next state Assigned is {nextStateName}";

                _logger.Warning(error);
                return false;
            }

            catch (Exception e) {
                error = $"FSM \"{Name}\". Failed to add steate transition " +
                    $"from {currentState} to {nextState} on {status}. " +
                    $"Exception {e.Message}";
                _logger.Error(error);

                return false;     
            }
        }

        public bool AddTransition(StateEnumBase currentState,
                        FsmStateStatus status, StateEnumBase nextState)
        {
            string error = String.Empty;

            try {
                
                if (States[currentState] == null) {

                    error = $"FSM \"{Name}\". Can't add transition from state {currentState.Name} " +
                        $"to {nextState.Name} on {status}. " +
                        $"Current state {currentState.Name} object does not exist.";
                    _logger.Error(error);
                    return false;
                }

                if (States[nextState] == null) {

                    error = $"FSM \"{Name}\". Can't add transition from state {currentState.Name} " +
                        $"to {nextState.Name} on {status}. " +
                        $"Next state {nextState.Name} object does not exist. ";
                    _logger.Error(error);
                    return false;
                }

                var trigger = new TransitionTrigger(States[currentState], status);

                if (!_transitionHandler.ContainsTrigger(trigger)) {

                    AddTransition(States[currentState], status, States[nextState]);
                    return true;
                }
                else {

                    if (_transitionHandler.PeekNextStateName(trigger,
                                             out string nextStateName)) {

                        if (nextStateName.Equals(nextState.Name)) {
                            _logger.Warning($"Attempt to add state " +
                                $"transition which is already added. " +
                                $"{trigger} / {nextState.Name}. Request ignored.");
                            return true;
                        }
                    }

                    error += $"FSM \"{Name}\". Transition from current state " +
                        $"{currentState.Name} on trigger " +
                        $"{currentState.Name} / {status} already exists.\n" +
                        $"Next state Assigned is {nextStateName}";

                    _logger.Warning(error);
                    return false;
                }

            }
            catch (Exception e) {
                error = $"FSM \"{Name}\". Failed to add steate transition " +
                    $"from {currentState.Name} to {nextState.Name} on {status}. " +
                    $"Exception {e.Message}";
                _logger.Error(error);
                return false;
            }    
        }

        public int AddTransition( StateBase currentState, 
                                  FsmStateStatus status, 
                                  StateBase nextState)
        {

            if (!States.ContainsKey(currentState.ID))
                States.Add(currentState);

            if (!States.ContainsKey(nextState.ID))
                States.Add(nextState);

            try {

                TransitionTrigger trigger = 
                    new TransitionTrigger(currentState, status);

                if (_transitionHandler.ContainsTrigger(trigger)) {

                    _logger.Warning($"FSM \"{Name}\". AddTransition(). " +
                                $"Trigger {currentState.Name } / " +
                                $"{status} already added.");
                }
                else {

                    _transitionHandler.AddTransition(new TransitionTrigger(currentState, status), nextState);
                }
            }
            catch (Exception ex) {

               _logger.Error($"FSM  \"{Name}\". AddTransition() Failed " +
                             $"to add transition from state {currentState.Name} " +
                              $"on trigger {status.ToString()} to" +
                              $" state {nextState.Name}. Exception: {ex.Message}");
                throw ex;
            }

            return _transitionHandler.Count;
        }

        ///<summary> This function starts FSM. Is executed in the 
        ///same thread where TauDevice object created. 
        /// 
        ///</summary>
        public bool StartFSMEngine()
        {

            // If there is no thread or it is not alive.
            _logger.Debug($"FSM \"{Name}\" engine: starting.");

            if ((_fsmWorkerThread != null) && (_fsmWorkerThread.IsAlive)) {
                _logger.Debug($"FSM \"{Name}\". Request to start ignored. " +
                                    $"Worker thread is already running.");
                return true;  // already running.
            }

            _logger.Debug($"FSM \"{Name}\" Engine: " +
                                $"starting new worker thread.");

            try {
                _threadCts = new CancellationTokenSource();
                _threadCt = _threadCts.Token;
                
                _fsmWorkerThread = new Thread(_FSMEngine);  
                _fsmWorkerThread.Start(); 

                if (_VerifyThreadRuns()) {

                    _logger.Information($"FSM \"{Name}\" Engine: worker " +
                        $"thread has started.");
                    ResetTimeOutClock();
                    return true;
                }
                else {

                    _logger.Error($"FSM \"{Name}\" Engine: " +
                        $"failed to start worker thread.");
                    return false;
                }
            }
            catch (Exception ex) {

                _logger.Error($"FSM \"{Name}\" Engine. " +
                    $"Failed to create or start worker thread. {ex.Message}");
                return false;
            }
        }

        private bool _VerifyThreadRuns()
        {
            int counter = 0;
            for (int i = EngineStartTimeout; i > 0; i -= EngineStartPeriodMs) {

                counter = i;

                if (FsmIsRunning)
                    break;
                Thread.Sleep(EngineStartPeriodMs);
            }
            return (counter >= 0);
        }


        private FsmEngineIdlingResult _Idling()
        {
            if (CommandPending) {
                return FsmEngineIdlingResult.CommandPending;
            }

            if (_fsmWorkerResetEvent == null) {
                return FsmEngineIdlingResult.ContineousRun;
            }

            _fsmWorkerResetEvent.WaitOne();

            lock (_fsmWorkerThreadLock) {

                FsmEngineIdlingResult r = CommandPending?
                    FsmEngineIdlingResult.CommandPending :
                    _timerFired ? FsmEngineIdlingResult.Tick : FsmEngineIdlingResult.IdlingInterrupted;

                _timerFired = false;

                return r;
            }
        }

        public  void ResetTimeOutClock( int roundTInMs = 100, int offsetInMs = 0 )
        {
            DateTime dt  = DateTime.Now;
            long ldt = dt.Ticks;
            dt = new DateTime( dt.Year, dt.Month, dt.Day, 
                               dt.Hour, dt.Minute, dt.Second, 
                               roundTInMs * ((dt.Millisecond) / roundTInMs));

            dt = dt.AddMilliseconds(roundTInMs + offsetInMs);

            _engineTimeOutClock = dt.Ticks;

            _missedTicksCounter = 0;
            _maxFsmExecTime = 1;
        }

        public bool ResumeFsmThread()
        {
            lock (_fsmWorkerThreadLock) {

                if ((_fsmWorkerThread == null) ||
                     (_fsmWorkerThread.ThreadState == ThreadState.Unstarted)) {
                    
                    _logger.Warning($"FSM \"{Name}\": an attempt to resume " +
                        $"nonexisting or unstarted worker thread.");
                    return false;
                }

                if (_fsmWorkerThread.ThreadState == ThreadState.WaitSleepJoin) {
                    _fsmWorkerResetEvent?.Set();
                }

                return true;
            }
        }

        public bool AbortWorkerThread()
        {
            try {
                _fsmWorkerThread.Abort();
                return true;
            } 
            catch (ThreadAbortException e) {
                _logger.Information($"FSM \"{Name}\" worker thread " +
                    $"aborted. {e.Message}");
                return true;
            }
            catch (Exception e) {
                _logger.Error($"FSM \"{Name}\" worker thread abort " +
                    $"error. {e.Message}");
                return false;
            }
        }

        public bool JoinWorkerThread(
                    int timeoutMs = DefaultWorkerTerminationTimeoutMs, 
                    bool abortIfTimeout = true)
        {
            try {

                if ( timeoutMs <= 0) {
                    _fsmWorkerThread.Join();
                    return true;
                }
                else {                    
                    if (!_fsmWorkerThread.Join(
                        TimeSpan.FromMilliseconds(timeoutMs)) ) {

                        _logger.Warning($"FSM \"{Name}\": worker thread " +
                            $"failed to stop within {timeoutMs}ms.");
                            
                        if (abortIfTimeout) {
                            _logger.Warning($"FSM \"{Name}\". Aborting " +
                                $"worker thread.");
                            return AbortWorkerThread();
                        }
                        return false;
                    }
                }
                return true;
            }
            catch ( ThreadAbortException ) {
                _logger.Information($"FSM \"{Name}\" worker thread aborted.");
                return true;
            }
            catch (Exception e) {
               _logger.Error($"Failed to join FSM \"{Name}\" worker thread. " +
                                $"Exception {e.Message}.");
                return false;
            }
        }
        #endregion Public Methods
    }
}
