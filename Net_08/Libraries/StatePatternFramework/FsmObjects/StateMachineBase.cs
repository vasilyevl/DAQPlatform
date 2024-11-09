using Grumpy.DaqFramework.Common;

using Microsoft.Extensions.Logging;

using System.Runtime.CompilerServices;

namespace Grumpy.StatePatternFramework
{
    [Flags]
    public enum IdlingResult
    {
        NA = 0,
        IdlingInterrupted = 1,
        CommandPending = 2,
        Tick = 4,
        Timeout = 8,
        ContineousRun = 16
    }

    public class StateChangeEventArgs : EventArgs {

        private EnumBase? _newState;
        private EnumBase? _previousState;

        public StateChangeEventArgs( EnumBase newState, 
                                     EnumBase? previousState = null )
        {
            _previousState = previousState;
            _newState = newState;
        }

        public  EnumBase? PreviousState => _previousState;
        public EnumBase? NewState => _newState;
    }

    public class StateProcArgs: EventArgs
    {
        public StateProcArgs(): base()
        {
            IdlingResult = IdlingResult.NA;
        }

        public StateProcArgs(IdlingResult st ) : base()
        {
            IdlingResult = st;
        }

        public IdlingResult IdlingResult { get; set; }
    }

    public delegate void StateChangeEventHandler(object sender, 
                                                 StateChangeEventArgs e);
    public class StateMachineBase
    {
        protected ILogger? _logger = null;

        protected const int EngineStartTimeout = 50;
        protected const int EngineStartPeriodMs = 1;
        protected const int DefaultWorkerTerminationTimeoutMs = 5000;
        protected const int _StateHistoryDepth = 32;

        #region Members

        protected StackBase<StateBase>? _statesHistory;
        protected StateTransitionManager _transitionManager;
        // FSM thread.
        protected Thread? _workerThread;                 
        AutoResetEvent? _fsmWorkerResetEvent;
        protected object _workerLock;

        private CancellationTokenSource? _threadCts;

        private Timer? _idlingTimer;
        // Maximum time it took to Execute any of the states in FSMPrdMs. 
        protected long _maxStateExecutionTime;
        // Keeps number of times it took longer than _periodInTcks
        // to execute any state function.
        protected long _missedTriggerCounter;
        // Time keeper. 
        protected long _engineTimeOutClock;             

        protected object? _stateLock;

        private IdlingResult _lastIdlingResult;

        private object _idlingCallBackLock;
        private bool _timerFired;

        private object _carrentStateLock;
        private StateBase? _currentState;
        public StateQueue StateQueue { get; protected set; }
        #endregion Members

        public event StateChangeEventHandler? StateChangeEvent;

        #region Constructors:
        /// <summary> Constructor with Name only  as a parameter. 
        ///  </summary>Such device will be considered independent or master.
        public StateMachineBase(string name, bool logTransitions = false, 
                       ILogger? logger = null)
        {
            Name = name;        
            _logger = logger;
            _carrentStateLock = new object();
            _idlingCallBackLock = new object();
            _workerLock = new object();

            _workerThread = null;

            _maxStateExecutionTime = 0;
            _missedTriggerCounter = 0;
            _engineTimeOutClock = 0;
            _lastIdlingResult = IdlingResult.NA;
            _timerFired = false;

            _threadCts = new CancellationTokenSource();           
            _transitionManager = new StateTransitionManager();

            LogTransitions = logTransitions;
            StateQueue = new StateQueue();

            _fsmWorkerResetEvent = null;

            _InitFSM();

            // This virtual method adds custom states to the dictionary
            // and defines transitions.
            try {

                InitFSM();
            }
            catch (Exception ex) {

                string msg = $"FSM \"{Name}\". InitFSM() call failed. " +
                    $"Check Transition table. Exception: {ex.Message}";
                _logger?.LogError(msg);
                throw new Exception(msg);
            }
        }

        public string Name { get; set; }
        public bool InStateSequence => StateQueue.Count > 0;

        public StateBase? CurrentState {
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
            _transitionManager = new StateTransitionManager();

            States = [];
            // The first state always:
            AddState(new StartState(this));
            // These dummy states used FSM stopping.
            AddState(new EndState(this));
            AddState(new StopState(this));
            CurrentState = States[StateIDBase.Start];
            _statesHistory = new StackBase<StateBase>(_StateHistoryDepth);
        }

        virtual public void InitFSM() { }

        virtual public bool CommandPending => false;

        #region Public Properties:
        public StateLibrary? States {
            get; private set;
        }

        public bool LogTransitions { get; set;  }

        /// <summary> True if FSM worker thread is alive 
        /// (not terminated or aborted).
        /// </summary>
        public bool FsmIsRunning => 
            _workerThread?.IsAlive ?? false;  

        public bool WorkerIsPaused => 
            (_workerThread != null) ? 
                _workerThread.ThreadState == ThreadState.WaitSleepJoin :
                false;

        public bool FsmIsSuspended => 
            (_workerThread != null) ? 
                _workerThread.ThreadState == ThreadState.Suspended :
                false;

        public void ResetMaxTimeRecord() => _maxStateExecutionTime = 0;

        public long MaxExecTime {

            get => _maxStateExecutionTime;
            protected set => _maxStateExecutionTime = 0;   
        }

        public long NumberOfMissedTicks {

            get =>(_missedTriggerCounter); 
            protected set => _missedTriggerCounter = value;
        }

        public StateTransitionManager? TransitionManager => 
                                            _transitionManager;
        
        public bool MissedTicks => _missedTriggerCounter > 0; 
        #endregion // Public Properties:

        #region Protected Methods       
        protected int AddState(StateBase state)
        {
            if( States == null) {

                States = new StateLibrary();
            }

            if (States.ContainsKey(state.ID)) {

                string err = $"FSM \"{Name}\". Failed to add state " +
                    $"{state.Name} to the list";

                _logger?.LogError(err);
                
                throw new Exception(err);
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
                catch (Exception fsmEx) {

                    errCntr++;

                    var msg = $"FSM \"{Name}\". Failed to add state " +
                    $"{st.Name} to the list. Exception: {fsmEx.Message}";

                    _logger?.LogError(msg);

                    if (_logger is null) {

                        throw new Exception(msg);
                    }
                }
            }

            return statesAdded;
        }

        protected virtual void RaiseStateChangeEvent( StateIDBase newState,
                                                      EnumBase previousState)
        {
            if (StateChangeEvent != null) {

                Delegate[] invocationList = 
                                StateChangeEvent.GetInvocationList();

                if (States is not null && States[newState] is not null) {

                    if (StateChangeEvent != null) {

                        Task.Factory.StartNew(() => {
                                StateChangeEvent.Invoke( this, 
                                    new StateChangeEventArgs(newState, 
                                                         previousState));}, 
                                TaskCreationOptions.LongRunning 
                        );
                    }
                }
                else {
                    // "Stop" is a special state, which FSM machine might not need to switch to.
                    // It can simply stop FSM engine when switch to "Stop" requested.
                    if (!newState.Name.Equals("Stop", 
                                    StringComparison.OrdinalIgnoreCase)) {

                        throw new Exception($"FSM \"{Name}\". " +
                            $"RaiseStateChangeEvent(). State {newState} " +
                            $"not registered.");
                    }
                }
            }
        }
        #endregion Protected Methods

        #region Public Methods:
        public bool GetState(string name, ref StateBase? st)
        {
            if ( States is null || States.Count < 1) {
                
                st = null;
                return false;
            }

            st = States[name];
            return st is not null;
        }

        public StackBase<StateBase>? HistoryStack => _statesHistory;

        public List<StateBase> StateHistory => 
            _statesHistory?.PeekAllAsList() ?? new List<StateBase>();

        public StateBase? PreviousState => 
            _statesHistory?.Peek(out StateBase? last) ?? false? last : null;

        private void _IntitNewState(StateBase nextState)
        {
            if (nextState is null) {

                throw new NullReferenceException($"New state is not " +
                    $"selectcted. FSM \"{Name}\", Current " +
                    $"state {CurrentState?.Name}");
            }
            
            _idlingTimer?.Dispose();
            
            if (CurrentState is not null) {

                HistoryStack?.Push(CurrentState, force: true);
            }
            
            CurrentState = nextState;
            CurrentState.ActivateState();
            CurrentState.Enter();

            lock (_workerLock) {

                if (CurrentState.TimeoutIsInfinite || 
                    CurrentState.UsesWatchDog) {

                    _fsmWorkerResetEvent = 
                        new AutoResetEvent(initialState: false);
                }
                else {
                    _fsmWorkerResetEvent?.Close();
                    _fsmWorkerResetEvent = null;
                }
            }

            if (CurrentState.UsesWatchDog) {
                _idlingTimer = 
                    new Timer(_IdlingWakeUpCallBack!, null, 
                              CurrentState.PeriodMS, CurrentState.PeriodMS);
            }
        }


        public void _IdlingWakeUpCallBack(object info)
        {
            bool lockTaken = false;

            try {

                Monitor.TryEnter(_idlingCallBackLock!, 0, ref lockTaken);

                if (lockTaken) {

                    lock (_workerLock) {
                        // Resume thread if paused.
                        if (FsmIsRunning && WorkerIsPaused) {
                            _timerFired = true;
                            _fsmWorkerResetEvent?.Set();
                        }
                    }
                }
                else {
                    _missedTriggerCounter++;
                    _logger?.LogDebug($" FSM \"{Name}\": Watchdog timer " +
                        $"failed to enter callback.");
                }
            }
            catch(Exception e) {
                
                _missedTriggerCounter++;
                _logger?.LogWarning($"FSM \"{Name}\". _IdlingWakeUp() exception: {e.Message}");
            }
            finally {
                
                if (lockTaken) {
                
                    Monitor.Exit(_idlingCallBackLock!);
                }
            }
        }

        private bool _KeepGoing() => !(_threadCts?.Token.IsCancellationRequested ?? false) &&
                                     CurrentState is not null &&
                                     CurrentState.Name != "Stop" &&
                                     CurrentState.Name != "End";

        protected virtual void _FSMEngine()
        {
            // Reset timeout marker.
            _engineTimeOutClock = DateTime.Now.Ticks;

            if (CurrentState is null) {

                string msg = $"FSM \"{Name}\" failed to start. " +
                    $"Intial state is not set.";
                _logger?.LogError(msg);

                if (_logger is null) {

                    throw new Exception(msg);
                }

                return;
            }

            //Enter Imnitial state...  
            CurrentState.Enter();

            _logger?.LogInformation($"FSM \"{Name}\" engine started. " +
                $"{CurrentState.Name} initial state entered.");

            _lastIdlingResult = IdlingResult.NA;

            while (_KeepGoing()) { 

                while (CurrentState.IsActive) {

                    CurrentState.StateProc(
                                new StateProcArgs(_lastIdlingResult) );
                   
                    if (CurrentState.IsActive) {

                        _lastIdlingResult = 
                            CurrentState.UsesIdling ? 
                                _Idling() : 
                                IdlingResult.ContineousRun;
                    }

                    if (_threadCts?.Token.IsCancellationRequested ?? false) {

                        _logger?.LogWarning($"FSM \"{Name}\" " +
                            $"cancellation requested. " +
                            $"Exitting FSM thread worker.");
                        return;
                    }
                }

                _DisarmIdlingTimer();

                CurrentState.Exit();
                
                if (_SelectNextState(out StateBase? nextState) 
                    && nextState is not null) {

                    EnumBase previousStateID = CurrentState.ID;
                    _IntitNewState(nextState);
                    RaiseStateChangeEvent(CurrentState.ID, previousStateID);
                }
                else {
                    // Error. Exit worker.
                    break;
                }
            }          
            _logger?.LogInformation($"Stopping {Name}  FSM Engine.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _DisarmIdlingTimer()
        {
            try {

                Monitor.Enter(_idlingCallBackLock);

                _idlingTimer?.Dispose();
                _idlingTimer = null;
                _lastIdlingResult = IdlingResult.NA;
            }
            catch ( Exception ex_) {

                _logger?.LogWarning($"FSM \"{Name}\". Exception while " +
                    $"disarming idling timer. Exception: {ex_.Message}");
            }
            finally {
                
                Monitor.Exit(_idlingCallBackLock);
            }
        }

        private bool _SelectNextState(out StateBase? nextState)
        {
            if (StateQueue is not null && StateQueue.Count > 0) {

                StateQueue.TryDequeue(out nextState);

                if (nextState is null) {

                    var error = $"FSM \"{Name}\". Failed to select next " +
                        $"state from the state queue.";

                    if (_logger is not null) {

                        _logger?.LogError(error);
                    }
                    else {

                        throw new Exception(error);
                    }
                }
            }
            else {

                nextState = null;

                try {

                    nextState = 
                        _transitionManager?.NextState(CurrentState!) ?? null;
                }
                catch {

                    var error = $"FSM \"{Name}\". Failed to select next " +
                        $"state for {CurrentState?.Name} " +
                        $"/ {CurrentState?.Result}";


                    if (States?.ContainsKey(StateIDBase.TransitionError) 
                        ?? false) {

                        nextState = States[StateIDBase.TransitionError];
                    }
                    else {

                        error += $"\n\"TransitionError\" state is not defined.";

                        if (_logger is not null) {

                            _logger?.LogError(error);
                        }
                        else {

                            throw new Exception(error);
                        }
                    }
                }

                if (LogTransitions) {

                    var msg = $"FSM \"{Name}\" Engine. Transition from " +
                        $"state \"{(CurrentState?.Name ?? "None")}\" " +
                        $"on trigger \"{CurrentState?.Result}\" " +
                        $"to state \"{(nextState?.Name ?? "None")}\"";

                    if ((nextState?.ID ?? StateIDBase.NA) 
                        == StateIDBase.TransitionError) {

                        _logger?.LogWarning(msg);
                    }
                    else {

                        _logger?.LogInformation(msg);
                    }
                }
            }

            return nextState is not null;
        }
    
        virtual protected bool CustomTransitionHandler(
                                    out StateBase? nextState)
        {
            nextState = null;
            
            if (StateQueue.Count > 0) {

                StateQueue.TryDequeue(out nextState);
            }
            
            return (nextState is not null);
        }

        public int AddTransitions( List<Tuple<StateBase, 
                                   StateExecutionResult, 
                                   StateBase>> transitions)
        {
            foreach (var item in transitions) {

                AddTransition(item.Item1, item.Item2, item.Item3);
            }

            return _transitionManager?.Count ?? 0;
        }

        public bool AddTransition ( string currentState, 
                                    StateExecutionResult status, 
                                    string nextState)
        {
            string error = String.Empty;
            
            try {

                if ( States is null || States[currentState] is null) {
                    
                    error = $"FSM \"{Name}\". Can't add transition " +
                        $"from state {currentState} to {nextState} " +
                        $"on {status}. Current state {currentState} " +
                        $"object does not exist.";
                    
                    _logger?.LogError(error);

                    if (_logger is null) {
                    
                        throw new Exception(error);
                    }

                    return false;
                }

                if (States is null || States[nextState] is null) {
                    
                    error = $"FSM \"{Name}\". Can't add transition from " +
                        $"state {currentState} to {nextState} on {status}. " + 
                        $"Next state {nextState} object does not exist. ";

                    _logger?.LogError(error);
                    
                    if (_logger is null) {
                    
                        throw new Exception(error);
                    }
                    
                    return false;
                }

                var trigger = 
                    new TransitionTrigger(States[currentState]!, status);

                if (!_transitionManager.ContainsTrigger(trigger)) {

                    AddTransition( States[currentState]!, 
                                   status, 
                                   States[nextState]!);
                    
                    return true;
                }

                if (_transitionManager.PeekNextStateName(trigger, 
                                         out string nextStateName)) {

                    if (nextStateName.Equals(nextState)) {
                        
                        _logger?.LogWarning($"FSM \"{Name}\". " +
                            $"Attempt to add state " +
                            $"transition which is already added. " +
                            $"{trigger} / {nextState}. Request ignored.");
                        
                        return true;
                    }
                }

                error += $"FSM \"{Name}\". Transition from current state " +
                    $"{ currentState} on trigger \"{currentState} / " +
                    $"{status}\" already added.\nNext state " +
                    $"assigned is {nextStateName}";

                _logger?.LogWarning(error);
                
                return false;
            }

            catch (Exception e) {
                
                error = $"FSM \"{Name}\". Failed to add steate transition " +
                    $"from {currentState} to {nextState} on {status}. " +
                    $"Exception {e.Message}";
                
                _logger?.LogError(error);

                if (_logger is null) {
                
                    throw new Exception(error);
                }

                return false;     
            }
        }

        public bool AddTransition(StateIDBase currentState,
                                  StateExecutionResult status, 
                                  StateIDBase nextState)
        {
            if (_transitionManager is null) {
                
                _transitionManager = new StateTransitionManager();
            }   

            string error = String.Empty;

            try {
                
                if (States is null || States[currentState] is null) {

                    error = $"FSM \"{Name}\". Can't add transition " +
                        $"from state {currentState.Name} " +
                        $"to {nextState.Name} on {status}. " +
                        $"Current state {currentState.Name} object " +
                        $"does not exist.";

                    _logger?.LogError(error);

                    if (_logger is null) {

                        throw new Exception(error);
                    }
                    return false;
                }

                if (States[nextState] is null) {

                    error = $"FSM \"{Name}\". Can't add transition from " +
                        $"state {currentState.Name} to {nextState.Name} " +
                        $"on {status}. Next state {nextState.Name} " +
                        $"object does not exist. ";
                    
                    _logger?.LogError(error);

                    if (_logger is null) {

                        throw new Exception(error);
                    }

                    return false;
                }

                var trigger = new TransitionTrigger(States[currentState], 
                                                    status);

                if (!_transitionManager?.ContainsTrigger(trigger) ?? false) {

                    AddTransition(States[currentState], status, 
                                  States[nextState]);
                    return true;
                }
                else {

                    if (_transitionManager?.PeekNextStateName(trigger,
                                   out string nextStateName) ?? false) {

                        if (nextStateName.Equals(nextState.Name)) {

                            _logger?.LogWarning($"Attempt to add state " +
                                $"transition which is already added. " +
                                $"{trigger} / {nextState.Name}. " +
                                $"Request ignored.");
                            
                            return true;
                        }
                    }

                    error += $"FSM \"{Name}\". Transition from current " +
                        $"state {currentState.Name} on trigger " +
                        $"{currentState.Name} / {status} already exists.\n" +
                        $"Next state Assigned is {nextState.Name}";

                    _logger?.LogWarning(error);

                    return false;
                }
            }
            catch (Exception e) {

                error = $"FSM \"{Name}\". Failed to add steate transition " +
                    $"from {currentState.Name} to {nextState.Name} " +
                    $"on {status}. Exception {e.Message}";
                
                _logger?.LogError(error);
                
                if (_logger is null) {
                
                    throw new Exception(error);
                }

                return false;
            }    
        }

        public int AddTransition( StateBase currentState, 
                                  StateExecutionResult status, 
                                  StateBase nextState)
        {
            if( _transitionManager is null) {
                
                _transitionManager = new StateTransitionManager();
            }

            if (States is null) {
                
                States = new StateLibrary();
            }

            if (!States.ContainsKey(currentState.ID)) {
         
                States.Add(currentState);
            }

            if (!States.ContainsKey(nextState.ID)) {

                States.Add(nextState);
            }

            try {

                TransitionTrigger trigger = 
                    new TransitionTrigger(currentState, status);

                if (_transitionManager?.ContainsTrigger(trigger) ?? false) {

                    _logger?.LogWarning($"FSM \"{Name}\". AddTransition(). " +
                                $"Trigger {currentState.Name } / " +
                                $"{status} already added.");
                }
                else {

                    _transitionManager?.AddTransition(
                        new TransitionTrigger(currentState, status), 
                        nextState);
                }
            }
            catch (Exception ex) {

                var error = $"FSM \"{Name}\". AddTransition() Failed to add " +
                            $"transition from state {currentState.Name} " +
                            $"on trigger {status.ToString()} to state " +
                            $"{nextState.Name}. Exception: {ex.Message}";

                _logger?.LogError(error);
               
                throw new Exception(error, ex);
            }

            return _transitionManager?.Count ?? 0;
        }

        ///<summary> This function starts FSM. Is executed in the 
        ///same thread where TauDevice object created. 
        /// 
        ///</summary>
        public bool StartFSMEngine()
        {
            // If there is no thread or it is not alive.
            _logger?.LogDebug($"FSM \"{Name}\" engine: starting.");

            if ((_workerThread != null) && (_workerThread.IsAlive)) {

                _logger?.LogDebug($"FSM \"{Name}\". " +
                    $"Request to start ignored. " +
                    $"Worker thread is already running.");

                return true;  // already running.
            }

            _logger?.LogDebug($"FSM \"{Name}\" Engine: " +
                                $"starting new worker thread.");

            try {

                _threadCts = new CancellationTokenSource();
                _workerThread = new Thread(_FSMEngine);  
                _workerThread.Start(); 

                if (_VerifyThreadRuns()) {

                    _logger?.LogInformation($"FSM \"{Name}\" " +
                        $"Engine: worker thread has started.");
                    
                    ResetTimeOutClock();
                    
                    return true;
                }
                else {

                    var error = $"FSM \"{Name}\" Engine: " +
                        $"failed to start worker thread.";

                    _logger?.LogError(error);

                    if (_logger is null) {
                
                        throw new Exception(error);
                    }

                    return false;
                }
            }
            catch (Exception ex) {

                var error = $"FSM \"{Name}\" Engine: " +
                    $"failed to start worker thread. {ex.Message}";

                _logger?.LogError(error);

                if (_logger is null) {

                    throw new Exception(error, ex);
                }

                return false;
            }
        }

        private bool _VerifyThreadRuns()
        {
            int counter = 0;
            
            for (int i = EngineStartTimeout; i > 0; i -= EngineStartPeriodMs) {

                counter = i;

                if (FsmIsRunning) {
                
                    break;
                }

                Thread.Sleep(EngineStartPeriodMs);
            }

            return (counter >= 0);
        }

        private IdlingResult _Idling()
        {
            if (CommandPending) {

                return IdlingResult.CommandPending;
            }

            if (_fsmWorkerResetEvent == null) {

                return IdlingResult.ContineousRun;
            }

            if( _threadCts != null 
                && _threadCts.Token.IsCancellationRequested) {

                return IdlingResult.IdlingInterrupted;
            }

            _fsmWorkerResetEvent.WaitOne();

            lock (_workerLock) {

                IdlingResult r = CommandPending?
                    IdlingResult.CommandPending :
                        _timerFired ? 
                            IdlingResult.Tick : 
                            IdlingResult.IdlingInterrupted;

                _timerFired = false;

                return r;
            }
        }

        public  void ResetTimeOutClock( int roundTInMs = 100, 
                                        int offsetInMs = 0 )
        {
            DateTime dt  = DateTime.Now;
            long ldt = dt.Ticks;
            dt = new DateTime( dt.Year, dt.Month, dt.Day, 
                               dt.Hour, dt.Minute, dt.Second, 
                               roundTInMs * ((dt.Millisecond) / roundTInMs));

            dt = dt.AddMilliseconds(roundTInMs + offsetInMs);

            _engineTimeOutClock = dt.Ticks;
            _missedTriggerCounter = 0;
            _maxStateExecutionTime = 1;
        }

        public bool ResumeWorker()
        {
            lock (_workerLock) {

                if ((_workerThread == null) ||
                     (_workerThread.ThreadState == ThreadState.Unstarted)) {
                    
                    _logger?.LogWarning($"FSM \"{Name}\": an attempt " +
                        $"to resume nonexisting or unstarted worker thread.");

                    return false;
                }

                if (_workerThread.ThreadState == ThreadState.WaitSleepJoin) {

                    _fsmWorkerResetEvent?.Set();
                }

                return true;
            }
        }

        public bool AbortWorkerThread()
        {
            try {

                if(_threadCts is null) {

                    _threadCts = new CancellationTokenSource();
                }

                _threadCts?.Cancel();
                _workerThread?.Join();

                return true;
            } 
            catch (ThreadAbortException e) {

                _logger?.LogInformation($"FSM \"{Name}\" worker thread " +
                    $"aborted. {e.Message}");

                return true;
            }
            catch (Exception e) {

                String error = $"FSM \"{Name}\" worker thread abort " +
                    $"error. {e.Message}";

                if(_logger is null) {

                    throw new Exception(error);
                }
                else {

                    _logger.LogError(error);
                    return false;
                }               
            }
        }

        public bool JoinWorkerThread(
                    int timeoutMs = DefaultWorkerTerminationTimeoutMs,
                    bool abortIfTimeout = true)
        {
            try {

                if ( timeoutMs <= 0) {

                    _workerThread?.Join();
                    return true;
                }
                else { 
                    
                    if (!(_workerThread?.Join( 
                            TimeSpan.FromMilliseconds(timeoutMs)) ?? false) ) {

                        _logger?.LogWarning($"FSM \"{Name}\": worker thread " +
                            $"failed to stop within {timeoutMs}ms.");
                            
                        if (abortIfTimeout) {
                            _logger?.LogWarning($"FSM \"{Name}\". Aborting " +
                                $"worker thread.");
                            return AbortWorkerThread();
                        }

                        return false;
                    }
                }

                return true;
            }
            catch ( ThreadAbortException ) {

                _logger?.LogInformation($"FSM \"{Name}\" " +
                                        $"worker thread aborted.");
                return true;
            }
            catch (Exception e) {

                string error = $"FSM \"{Name}\" failed to " +
                    $"join worker thread. Exception {e.Message}";

                if(_logger is null) {

                    throw new Exception(error);
                }
                else {

                    _logger.LogError(error);
                    return false;
                }
            }
        }
        #endregion Public Methods
    }
}
