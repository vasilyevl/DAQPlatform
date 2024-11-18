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
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Grumpy.StatePatternFramework
{
    [Flags]
    public enum IdlingResult
    {
        NA = 0,
        IdlingInterrupted = 1 << 0,
        CommandPending = 1 << 1,
        Tick = 1 << 2,
        Timeout = 1 << 3,
        ContineousRun = 1 << 4,
        Any = IdlingInterrupted | CommandPending 
            | Tick | Timeout | ContineousRun
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
        protected const int EngineStartTimeout = 100;
        protected const int EngineStartCheckPeriodMs = 15;
        protected const int DefaultWorkerTerminationTimeoutMs = 10000;
        protected const int HistoryDepth = 128;
        protected const int DefaultCommandQueueCapacity = 256;

        #region Members
        protected ILogger? _logger = null;
        protected StackBase<StateBase>? _history;
        protected StateTransitionManager _transitionManager;
        
        protected Thread? _workerThread;                 
        AutoResetEvent? _workerResetEvent;
        protected object _workerLock;

        private CancellationTokenSource? _cts;

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

        private CommandQueue _pendingCommands;

        #endregion Members

        public event StateChangeEventHandler? StateChangeEvent;

        #region Constructors:
        /// <summary> Constructor with Name only  as a parameter. 
        ///  </summary>Such device will be considered independent or master.
        public StateMachineBase(string? name,
            bool logTransitions = false, 
            ILogger? logger = null) {

            Name = name ?? string.Empty;
            this._logger = logger;

            _carrentStateLock = new object();
            _idlingCallBackLock = new object();
            _workerLock = new object();

            _workerThread = null;

            _maxStateExecutionTime = 0;
            _missedTriggerCounter = 0;
            _engineTimeOutClock = 0;
            _lastIdlingResult = IdlingResult.NA;
            _timerFired = false;

            _cts = new CancellationTokenSource();

            _transitionManager =
                new StateTransitionManager();

            LogTransitions = logTransitions;
            StateQueue = new StateQueue();

            _workerResetEvent = null;
            _pendingCommands =
                new CommandQueue(DefaultCommandQueueCapacity);

            _stateLock = new object();
            _carrentStateLock = new object();
            _transitionManager = new StateTransitionManager();

            States = [];

            AddState(new StartState(this));
            // These dummy states used for state machine stopping.
            AddState(new EndState(this));
            AddState(new StopState(this));
            CurrentState = States[StateIDBase.Start];
            _history = new StackBase<StateBase>(HistoryDepth);

            try {
                // Add user states and transitions.
                InitStateMachine();
            }
            catch (Exception ex) {

                string msg = $"State machine \"{Name}\". " +
                    $"Failed to add user defined states and transitions. " +
                    $"Exception: {ex.Message}";

                logger?.LogError(msg);

                throw new Exception(msg);
            }
        }

        public StateQueue StateQueue { 
            get; 
            protected set; 
        }

        public string Name { 
            get; 
            private set; 
        }

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

        virtual public void InitStateMachine() { }

        public bool CommandPending => 
                (_pendingCommands?.Count ?? 0) > 0;



        public bool PurgeCommands() => _pendingCommands?.Purge() ?? true;


        #region Public Properties:

        public bool CanAddCommand =>
            (_pendingCommands?.Count ?? 0) < 
                    (_pendingCommands?.Depth ?? -1);

        public StateLibrary? States {
            get; 
            private set;
        }

        public bool LogTransitions { 
            get; 
            set;  
        }

        public bool IsRunning => 
            _workerThread?.IsAlive ?? false;  

        public bool IsPaused =>
            (_workerThread != null) 
            && _workerThread.ThreadState == ThreadState.WaitSleepJoin;

        public bool IsSuspended =>
            (_workerThread != null) 
            && _workerThread.ThreadState == ThreadState.Suspended;

        

        public long MaxSTateExecutionTime {

            get => _maxStateExecutionTime;
            protected set => _maxStateExecutionTime = 0;   
        }

        public long NumberOfMissedTicks {

            get => _missedTriggerCounter; 
            protected set => _missedTriggerCounter = value;
        }

        public bool MissedTicks => _missedTriggerCounter > 0;

        public StateTransitionManager? TransitionManager => 
                                            _transitionManager;


        #endregion // Public Properties:

        #region Protected Methods   

        protected CommandBase? PopCommand() =>
            (_pendingCommands?.TryDequeue(out CommandBase? cmd) ?? false)
            ? cmd
            : null;

        protected bool EnqueueCommand(CommandBase cmd) =>
            (_pendingCommands?.TryEnqueue(cmd) ?? false);


        protected int AddState(StateBase state)
        {
            if( States == null) { States = [];}

            if (States.ContainsKey(state.ID)) {

                string err = $"State Machine \"{Name}\". Failed to " +
                    $"add state {state.Name} to the list";

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

                    string msg = $"State machine \"{Name}\". " +
                        $"Failed to add state {st.Name} to the list. " +
                        $"Exception: {fsmEx.Message}";

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
                    // "Stop" is a special dummy state which indicates to the
                    // state machine that "Stop" requested.
                    if (!newState.Name.Equals("Stop", 
                                    StringComparison.OrdinalIgnoreCase)) {

                        throw new Exception($"State machine \"{Name}\". " +
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

        public List<StateBase> StateHistory => 
            _history?.PeekAllAsList() ?? new List<StateBase>();

        public StateBase? PreviousState => 
            _history?.Peek(out StateBase? last) ?? false? last : null;

        private void IntitNextState(StateBase nextState)
        {
            if (nextState is null) {

                throw new NullReferenceException(
                    $"State machine \"{Name}\". " +
                    $"New state is not selectcted. " +
                    $"Current state {CurrentState?.Name}");
            }
            
            _idlingTimer?.Dispose();
            
            if (CurrentState is not null) {

                _history?.Push(CurrentState, force: true);
            }
            
            CurrentState = nextState;
            CurrentState.ActivateState();
            CurrentState.Enter();

            lock (_workerLock) {

                if (CurrentState.TimeoutIsInfinite || 
                    CurrentState.UsesWatchDog) {

                    _workerResetEvent = 
                        new AutoResetEvent(initialState: false);
                }
                else {
                    _workerResetEvent?.Close();
                    _workerResetEvent = null;
                }
            }

            if (CurrentState.UsesWatchDog) {
                _idlingTimer = 
                    new Timer(IdlingWakeUpCallBack!, null, 
                              CurrentState.PeriodMS, CurrentState.PeriodMS);
            }
        }

        public void IdlingWakeUpCallBack(object info)
        {
            bool lockTaken = false;

            try {

                Monitor.TryEnter(_idlingCallBackLock!, 0, ref lockTaken);

                if (lockTaken) {

                    lock (_workerLock) {
                        // Resume thread if paused.
                        if (IsRunning && IsPaused) {
                            _timerFired = true;
                            _workerResetEvent?.Set();
                        }
                    }
                }
                else {
                    _missedTriggerCounter++;
                    _logger?.LogDebug($"State machine \"{Name}\": " +
                        $"Watchdog timer failed to enter callback.");
                }
            }
            catch(Exception e) {
                
                _missedTriggerCounter++;
                _logger?.LogWarning($"FSM \"{Name}\". IdlingWakeUp() " +
                    $"exception: {e.Message}");
            }
            finally {
                
                if (lockTaken) {
                
                    Monitor.Exit(_idlingCallBackLock!);
                }
            }
        }

        private bool CouldContinue() => 
            !(_cts?.Token.IsCancellationRequested ?? false) 
            && CurrentState is not null 
            && CurrentState.ID != StateIDBase.Stop 
            && CurrentState.ID != StateIDBase.End;

        protected virtual void Engine()
        {
            // Reset timeout marker.
            _engineTimeOutClock = DateTime.Now.Ticks;

            if (CurrentState is null) {

                string msg = $"State machine \"{Name}\" " +
                    $"failed to start. " +
                    $"Intial state is not set.";

                _logger?.LogError(msg);

                if (_logger is null) {
                    throw new Exception(msg);
                }

                return;
            }

            //Enter Imnitial state...  
            CurrentState.Enter();

            _logger?.LogInformation($"State machine " +
                $"\"{Name}\" engine started. ");

            _lastIdlingResult = IdlingResult.NA;

            while (CouldContinue()) { 

                while (CurrentState.IsActive) {

                    CurrentState.StateProc(
                        new StateProcArgs(_lastIdlingResult) );
                   
                    if (CurrentState.IsActive) {

                        _lastIdlingResult = 
                            CurrentState.UsesIdling ? 
                                Idling() : 
                                IdlingResult.ContineousRun;
                    }

                    if (_cts?.Token.IsCancellationRequested ?? false) {

                        _logger?.LogWarning($"State machine \"{Name}\" " +
                            $"cancellation requested.");
                        return;
                    }
                }

                DisarmIdlingTimer();

                CurrentState.Exit();
                
                if (SelectNextState(out StateBase? nextState) 
                    && nextState is not null) {

                    EnumBase previousStateID = CurrentState.ID;
                    IntitNextState(nextState);
                    RaiseStateChangeEvent(CurrentState.ID, previousStateID);
                }
                else {
                    // Error. Exit worker.
                    break;
                }
            }          
            _logger?.LogInformation($"State machine \"{Name}\" stopping engine.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DisarmIdlingTimer()
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

        private bool SelectNextState(out StateBase? nextState)
        {
            if ((StateQueue?.Count ?? 0) > 0) {

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
            return (nextState is not null);
        }

        public int AddTransitions( List<Tuple<StateBase, 
                                   StateResult, 
                                   StateBase>> transitions)
        {
            foreach (var item in transitions) {

                AddTransition(item.Item1, item.Item2, item.Item3);
            }

            return _transitionManager?.Count ?? 0;
        }

        public bool AddTransition ( string currentState, 
                                    StateResult status, 
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
                                  StateResult status, 
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
                                  StateResult status, 
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


        public bool StartEngine()
        {
            // If there is no thread or it is not alive.
            _logger?.LogDebug($"State machine \"{Name}\" engine: starting.");

            if ((_workerThread != null) && (_workerThread.IsAlive)) {

                _logger?.LogDebug($"State machine \"{Name}\". " +
                    $"Request to start ignored. " +
                    $"Worker thread is already running.");

                return true;  // already running.
            }

            _logger?.LogDebug($"State machine \"{Name}\" Engine: " +
                                $"starting new worker thread.");

            try {

                _cts = new CancellationTokenSource();
                _workerThread = new Thread(Engine);  
                _workerThread.Start(); 

                if (EngineThreadIsRunning()) {

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

        private bool EngineThreadIsRunning()
        {
            for (int i = EngineStartTimeout/EngineStartCheckPeriodMs; i >= 0; i--) {

                if (IsRunning) { return true; }
                Thread.Sleep(EngineStartCheckPeriodMs);
            }

            return false;
        }

        private IdlingResult Idling()
        {
            if (CommandPending) { 
                return IdlingResult.CommandPending; 
            }

            if (_workerResetEvent == null) { 
                return IdlingResult.ContineousRun;
            }

            if( _cts != null 
                && _cts.Token.IsCancellationRequested) {

                return IdlingResult.IdlingInterrupted;
            }

            _workerResetEvent.WaitOne();

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
                    
                    _logger?.LogWarning($"State machine \"{Name}\": " +
                        $"an attempt to resume nonexisting or " +
                        $"unstarted worker thread.");

                    return false;
                }

                if (_workerThread.ThreadState == ThreadState.WaitSleepJoin) {

                    _workerResetEvent?.Set();
                }

                return true;
            }
        }

        public bool AbortWorkerThread()
        {
            try {

                _cts ??= new CancellationTokenSource();
                _cts.Cancel();
                _workerThread?.Join();
                return true;
            } 
            catch (ThreadAbortException e) {

                _logger?.LogInformation($"State machine \"{Name}\" " +
                    $"worker thread " +
                    $"aborted. {e.Message}");

                return true;
            }
            catch (Exception e) {

                String error = $"State machine \"{Name}\" " +
                    $"worker thread abort " +
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

        public bool JoinWorker(
                    int timeoutMs = 
                        DefaultWorkerTerminationTimeoutMs,
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

    public class StateMachine<TCommand>: 
        StateMachineBase where TCommand: CommandBase
    {
        public StateMachine(string? name,
                            bool logTransitions = false,
                            ILogger? logger = null) :
            base(name, logTransitions, logger) { }

        public override void InitStateMachine() =>
            base.InitStateMachine();
      
        public bool EnqueueCommand(TCommand cmd) =>
            base.EnqueueCommand(cmd);

        public new TCommand? PopCommand() =>
            base.PopCommand() as TCommand;
    }

    public class StateWorker
    {
        protected ILogger? _logger = null;

        private Thread? thread;
        private AutoResetEvent? resetEvent;
        private CancellationTokenSource cts;

        private readonly object workerLock = new();

        public StateWorker(ILogger? logger = null) {
            _logger = logger;
            cts = new CancellationTokenSource();
            resetEvent = new AutoResetEvent(false);

        }


        public void Start(Action<CancellationToken> workerLogic) {
            lock (workerLock) {
                if (thread != null) return;
                thread = new Thread(() => workerLogic(cts.Token));
                thread.Start();
            }
        }

        public void StopWorkerThread() {
            lock (workerLock) {
                cts?.Cancel();
                thread?.Join();
                thread = null;
            }
        }

        public bool PauseWorker() {
            lock (workerLock) {
         
                if (thread == null) return false;

                resetEvent?.Reset();
                return true;
            }
        }

        public bool ResumeWorker() {
            lock (workerLock) {

                if ( (thread == null) 
                    || (thread.ThreadState == ThreadState.Unstarted)) {

                    _logger?.LogWarning($"State Machine worker: " +
                        $"an attempt to resume nonexisting or " +
                        $"unstarted worker thread.");

                    return false;
                }

                if (thread.ThreadState == ThreadState.WaitSleepJoin) {

                    resetEvent?.Set();
                }

                return true;
            }
        }

        public bool IsRunning => thread?.IsAlive ?? false;

        public bool IsPaused =>
            (thread != null) && thread.ThreadState == ThreadState.WaitSleepJoin;

        public bool IsSuspended =>
            (thread != null) && thread.ThreadState == ThreadState.Suspended;
    }

}
