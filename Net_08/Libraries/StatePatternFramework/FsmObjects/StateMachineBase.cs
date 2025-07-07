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
using Grumpy.Common;
using Grumpy.Common.BaseObjects;
using Grumpy.SDAQFramework.Common;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Grumpy.StatePatternFramework
{
    /// <summary>
    /// Represents the base class for a state machine.
    /// </summary>
    public class StateMachineBase
    {
        /// <summary>
        /// The timeout for starting the engine.
        /// </summary>
        protected const int EngineStartTimeout = 100;
        /// <summary>
        /// The period in milliseconds to check the engine start.
        /// </summary>
        protected const int EngineStartCheckPeriodMs = 15;
        /// <summary>
        /// The default timeout for terminating the worker.
        /// </summary>
        protected const int DefaultWorkerTerminationTimeoutMs = 10000;
        /// <summary>
        /// The depth of the state history.
        /// </summary>
        protected const int HistoryDepth = 128;

        /// <summary>
        /// The default capacity of the command queue.
        /// </summary>
        protected const int DefaultCommandQueueCapacity = 256;

        #region Members
        /// <summary>
        /// The logger instance.
        /// </summary>
        protected ILogger? _logger = null;

        /// <summary>
        /// The history of states.
        /// </summary>
        protected StackBase<StateBase>? _history;

        /// <summary>
        /// The state transition manager.
        /// </summary>
        protected StateTransitionManager _transitionManager;

        /// <summary>
        /// The worker thread.
        /// </summary>
        protected Thread? _workerThread;

        /// <summary>
        /// The reset event for the worker.
        /// </summary>
        AutoResetEvent? _workerResetEvent;

        /// <summary>
        /// The lock object for the worker.
        /// </summary>
        protected object _workerLock;

        /// <summary>
        /// The cancellation token source.
        /// </summary>
        private CancellationTokenSource? _cts;

        /// <summary>
        /// The timer for idling.
        /// </summary>
        private Timer? _idlingTimer;

        /// <summary>
        /// The maximum state execution time in FSMPrdMs.
        /// </summary>
        protected long _maxStateExecutionTime;

        /// <summary>
        /// The counter for missed triggers. Keeps number of times it took 
        /// longer than _periodInTcks to execute any state function.
        /// </summary>
        protected long _missedTriggerCounter;

        /// <summary>
        /// The clock for engine timeout.
        /// </summary>
        protected long _engineTimeOutClock;

        /// <summary>
        /// The lock object for the state.
        /// </summary>
        protected object? _stateLock;

        /// <summary>
        /// The last idling exit trigger value.
        /// </summary>
        private IdlingExitTrigger _lastIdlingExitTrigger;

        /// <summary>
        /// The lock object for the idling callback.
        /// </summary>
        private object _idlingCallBackLock;

        /// <summary>
        /// Indicates whether the timer fired.
        /// </summary>
        private bool _timerFired;

        /// <summary>
        /// The lock object for the current state.
        /// </summary>
        private object _carrentStateLock;

        /// <summary>
        /// The current state.
        /// </summary>
        private StateBase? _currentState;

        /// <summary>
        /// The queue of pending commands.
        /// </summary>
        private CommandQueue _pendingCommands;

        #endregion Members

        /// <summary>
        /// Occurs when the state machine transitions from 
        /// one state to another.
        /// </summary>
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
            _lastIdlingExitTrigger = IdlingExitTrigger.NA;
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

        /// <summary>
        /// Gets the state queue.
        /// </summary>
        public StateQueue StateQueue { 
            get; 
            protected set; 
        }

        /// <summary>
        /// Gets the name of the state machine.
        /// </summary>
        public string Name { 
            get; 
            private set; 
        }

        /// <summary>
        /// Gets a value indicating whether the state machine 
        /// is in a state sequence.
        /// </summary>
        public bool InStateSequence => StateQueue.Count > 0;

        /// <summary>
        /// Gets the current state.
        /// </summary>
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

        /// <summary>
        /// Initializes the state machine, setting up initial states, 
        /// transitions, and any required resources.
        /// Override and call this method before starting the state machine.
        /// </summary>
        virtual public void InitStateMachine() { }

        /// <summary>
        /// Gets a value indicating whether a command is pending.
        /// </summary>
        public bool CommandPending => 
                (_pendingCommands?.Count ?? 0) > 0;

        /// <summary>
        /// Purges all pending commands from the command queue.
        /// </summary>
        /// <returns>true if the command queue was successfully purged; otherwise, false.</returns>
        public bool PurgeCommands() => _pendingCommands?.Purge() ?? true;


        #region Public Properties:
        /// <summary>
        /// Gets a value indicating whether a command can be added.
        /// </summary>
        public bool CanAddCommand =>
            (_pendingCommands?.Count ?? 0) < 
                    (_pendingCommands?.MaxDepth ?? -1);

        /// <summary>
        /// Gets the state library which contains all states.
        /// </summary>
        public StateLibrary? States {
            get; 
            private set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to log transitions.
        /// </summary>
        public bool LogTransitions { 
            get; 
            set;  
        }

        /// <summary>
        /// Gets a value indicating whether the state machine is running.
        /// </summary>
        public bool IsRunning => _workerThread?.IsAlive ?? false;

        /// <summary>
        /// Gets a value indicating whether the state machine is paused.
        /// </summary>
        public bool IsPaused =>
            (_workerThread?.ThreadState ?? ThreadState.Stopped) == 
            ThreadState.WaitSleepJoin;

        /// <summary>
        /// Gets a value indicating whether the state machine aka 
        /// current state is idling.
        /// </summary>
        public bool IsIdling =>
            (_workerThread != null) && IsRunning && !IsPaused;

        /// <summary>
        /// Gets a value indicating whether the state machine is stopping.
        /// </summary>
        public bool IsStopping =>
            (CurrentState?.ID ?? StateIDBase.NA) == StateIDBase.Stop;

          
        /// <summary>
        /// Gets a value indicating whether the state machine 
        /// worker thread is suspended.
        /// </summary>
        public bool IsSuspended =>
            (_workerThread != null) 
            && _workerThread.ThreadState == ThreadState.Suspended;

        /// <summary>
        /// Gets the maximum state execution time.
        /// </summary>
        public long MaxSTateExecutionTime {

            get => _maxStateExecutionTime;
            protected set => _maxStateExecutionTime = 0;   
        }

        /// <summary>
        /// Gets the number of missed ticks.
        /// </summary>
        public long NumberOfMissedTicks {

            get => _missedTriggerCounter; 
            protected set => _missedTriggerCounter = value;
        }

        /// <summary>
        /// Gets a value indicating whether there are missed ticks.
        /// </summary>
        public bool MissedTicks => _missedTriggerCounter > 0;

        /// <summary>
        /// Gets the state transition manager.
        /// </summary>
        public StateTransitionManager? TransitionManager => 
                                            _transitionManager;

        #endregion // Public Properties:

        #region Protected Methods   

        /// <summary>
        /// Pops a command from the queue.
        /// </summary>
        /// <returns>The popped command if available; 
        /// otherwise, null.</returns>
        protected CommandBase? PopCommand() =>
            (_pendingCommands?.TryDequeue(out CommandBase? cmd) ?? false)
            ? cmd
            : null;

        /// <summary>
        /// Enqueues a command to the command queue.
        /// </summary>
        /// <param name="cmd">The command to enqueue.</param>
        /// <returns>true if the command was successfully enqueued; 
        /// otherwise, false.</returns>
        protected bool EnqueueCommand(CommandBase cmd) =>
            (_pendingCommands?.TryEnqueue(cmd) ?? false);


        /// <summary>
        /// Adds a state to the state machine.
        /// </summary>
        /// <param name="state">The state to add.</param>
        /// <returns>The total number of states in the state machine after 
        /// the addition.</returns>
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

        /// <summary>
        /// Adds multiple states to the state machine from a list.
        /// </summary>
        /// <param name="states">The list of states to add.</param>
        /// <returns>The total number of states in the state machine 
        /// after the addition.</r
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

        /// <summary>
        /// Raises an event to signal that the state has changed.
        /// </summary>
        /// <param name="newState">The new state that the state machine has 
        /// ransitioned to.</param>
        /// <param name="previousState">The previous state before the 
        /// transition.</param>
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

        /// <summary>
        /// Gets the state by name.
        /// </summary>
        /// <param name="name">The name of the state.</param>
        /// <param name="st">The state to get.</param>
        /// <returns>true if the state was found; otherwise, false.</returns>
        public bool GetState(string name, ref StateBase? st)
        {
            if ( States is null || States.Count < 1) {
                
                st = null;
                return false;
            }

            st = States[name];
            return st is not null;
        }

        /// <summary>
        /// Gets the history of states that the state machine has 
        /// transitioned through.
        /// </summary>
        public List<StateBase> StateHistory => 
            _history?.PeekAllAsList() ?? new List<StateBase>();

        /// <summary>
        /// Gets the previous state before the current state transition.
        /// </summary>
        public StateBase? PreviousState => 
            _history?.Peek(out StateBase? last) ?? false? last : null;

        /// <summary>
        /// Initializes the next state.
        /// </summary>
        /// <param name="nextState">The next state to initialize.</param>
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

        /// <summary>
        /// The callback for the idling wake-up.
        /// </summary>
        /// <param name="info">The callback information.</param>
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

        /// <summary>
        /// Determines whether the state machine could continue.
        /// </summary>
        /// <returns>true if the state machine could continue; 
        /// otherwise, false.</returns>
        private bool CouldContinue() => 
            !(_cts?.Token.IsCancellationRequested ?? false) 
            && CurrentState is not null 
            && CurrentState.ID != StateIDBase.Stop 
            && CurrentState.ID != StateIDBase.End;

        /// <summary>
        /// The engine logic of the state machine.
        /// </summary>
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

            _lastIdlingExitTrigger = IdlingExitTrigger.NA;

            while (CouldContinue()) { 

                while (CurrentState.IsActive) {

                    CurrentState.StateProc(
                        new StateProcArgs(_lastIdlingExitTrigger) );
                   
                    if (CurrentState.IsActive) {

                        _lastIdlingExitTrigger = 
                            CurrentState.UsesIdling ? 
                                Idling() : 
                                IdlingExitTrigger.ContineousRun;
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

        /// <summary>
        /// Disarms the idling timer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DisarmIdlingTimer()
        {
            try {

                Monitor.Enter(_idlingCallBackLock);

                _idlingTimer?.Dispose();
                _idlingTimer = null;
                _lastIdlingExitTrigger = IdlingExitTrigger.NA;
            }
            catch ( Exception ex_) {

                _logger?.LogWarning($"FSM \"{Name}\". Exception while " +
                    $"disarming idling timer. Exception: {ex_.Message}");
            }
            finally {
                
                Monitor.Exit(_idlingCallBackLock);
            }
        }

        /// <summary>
        /// Selects the next state.
        /// </summary>
        /// <param name="nextState">The next state to select.</param>
        /// <returns>true if the next state was selected; 
        /// otherwise, false.</returns>
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

        /// <summary>
        /// Handles custom transitions.
        /// </summary>
        /// <param name="nextState">The next state to transition to.</param>
        /// <returns>true if the custom transition was handled; 
        /// otherwise, false.</returns>
        virtual protected bool CustomTransitionHandler(
                                    out StateBase? nextState)
        {
            nextState = null;
            return (nextState is not null);
        }

        /// <summary>
        /// Adds transitions to the state machine.
        /// </summary>
        /// <param name="transitions">The list of transitions to add.</param>
        /// <returns>The number of transitions added.</returns>
        public int AddTransitions( List<Tuple<StateBase, 
                                   StateResult, 
                                   StateBase>> transitions)
        {
            foreach (var item in transitions) {

                AddTransition(item.Item1, item.Item2, item.Item3);
            }

            return _transitionManager?.Count ?? 0;
        }

        /// <summary>
        /// Adds a transition to the state machine.
        /// </summary>
        /// <param name="currentState">The current state.</param>
        /// <param name="status">The status that triggers the transition.</param>
        /// <param name="nextState">The next state.</param>
        /// <returns>true if the transition was added; otherwise, false.</returns>
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

        /// <summary>
        /// Adds a transition to the state machine.
        /// </summary>
        /// <param name="currentState">The current state ID.</param>
        /// <param name="status">The status that triggers the transition.</param>
        /// <param name="nextState">The next state ID.</param>
        /// <returns>true if the transition was added; otherwise, false.</returns>
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

        /// <summary>
        /// Adds a transition to the state machine.
        /// </summary>
        /// <param name="currentState">The current state.</param>
        /// <param name="status">The status that triggers the transition.</param>
        /// <param name="nextState">The next state.</param>
        /// <returns>The number of transitions added.</returns>
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

        /// <summary>
        /// Starts the state machine engine, initiating the processing of 
        /// states and transitions.
        /// </summary>
        /// <returns>true if the engine started successfully; otherwise, 
        /// false.</returns>
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


        /// <summary>
        /// Checks if the engine thread is currently running.
        /// </summary>
        /// <returns>true if the engine thread is running; otherwise, 
        /// false.</returns>
        private bool EngineThreadIsRunning()
        {
            for (int i = EngineStartTimeout/EngineStartCheckPeriodMs; i >= 0; i--) {

                if (IsRunning) { return true; }
                Thread.Sleep(EngineStartCheckPeriodMs);
            }

            return false;
        }

        /// <summary>
        /// Handles the idling state of the state machine and determines 
        /// the exit trigger.
        /// </summary>
        /// <returns>The trigger that caused the idling state to 
        /// exit.</returns>
        private IdlingExitTrigger Idling()
        {
            if (CommandPending) { 
                return IdlingExitTrigger.CommandPending; 
            }

            if (_workerResetEvent == null) { 
                return IdlingExitTrigger.ContineousRun;
            }

            if( _cts != null 
                && _cts.Token.IsCancellationRequested) {

                return IdlingExitTrigger.IdlingInterrupted;
            }

            _workerResetEvent.WaitOne();

            lock (_workerLock) {

                IdlingExitTrigger r = CommandPending?
                    IdlingExitTrigger.CommandPending :
                        _timerFired ? 
                            IdlingExitTrigger.Tick : 
                            IdlingExitTrigger.IdlingInterrupted;

                _timerFired = false;
                return r;
            }
        }


        /// <summary>
        /// Resets the timeout clock for the state machine.
        /// </summary>
        /// <param name="roundTInMs">The round trip time in milliseconds.</param>
        /// <param name="offsetInMs">The offset time in milliseconds.</param>
        public void ResetTimeOutClock( int roundTInMs = 100, 
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

        /// <summary>
        /// Resumes the worker thread if it is paused.
        /// </summary>
        /// <returns>true if the worker thread was successfully resumed; 
        /// otherwise, false.</returns>
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


        /// <summary>
        /// Aborts the worker thread, stopping its execution immediately.
        /// </summary>
        /// <returns>true if the worker thread was successfully aborted; 
        /// otherwise, false.</returns>
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

        /// <summary>
        /// Waits for the worker thread to terminate.
        /// </summary>
        /// <param name="timeoutMs">The maximum time to wait for the worker 
        /// thread to terminate, in milliseconds.</param>
        /// <param name="abortIfTimeout">If true, aborts the worker thread 
        /// if the timeout is reached.</param>
        /// <returns>true if the worker thread terminated within the 
        /// timeout period; otherwise, false.</returns>
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

}
