using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Grumpy.StatePatternFramework
{
    public abstract class Fsm: StateMachineBase                                             
    {
        protected int _workerCycle;

        protected CommandQueue _commandQueue;      

        public Fsm(string deviceName, bool logTransitions = false, ILogger? logger = null) : 
            base(deviceName, logTransitions, logger)
        {
            _commandQueue = new CommandQueue();
            
            CurrentCommand = null;
        }

        protected CommandBase? _currentCommand;
        public CommandBase? CurrentCommand { 
            get => _currentCommand;
            private set => _currentCommand = value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearCurrentCommand() => CurrentCommand = null;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CurrentCommandSetState(CommandState state) {
            if (_currentCommand != null) {
                _currentCommand.State = state;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CurrentCommandSetSuccess() => 
            CurrentCommandSetState(CommandState.Success);


        public void CurrentCommandSetError() =>
            CurrentCommandSetState(CommandState.Error);

        public void CurrentCommandSetTimeout() =>
            CurrentCommandSetState(CommandState.Timeout);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CommandBase? PopCurrentCommand()
        {
            var cmd = CurrentCommand;
            ClearCurrentCommand();
            return cmd;
        }

        public ICommandState EnqueueCommand(CommandBase command)
        {
            try {
                if (_commandQueue.Push(command)) {

                    ResumeWorker();

                    command.State = CommandState.Pending;
                }
                else {

                    _logger?.LogWarning($"FSM_Device. Failed to enque " +
                        $"command {command.CommandType}. " +
                        $"Command queue might be at capacity.");
                    command.State = CommandState.Ignored;
                }
            }
            catch (Exception ex) {

                string error = $"FSM_Device. Failed to enque " +
                    $"command {command.CommandType}. " +
                    $"Exception: {ex.Message}";

                if (_logger != null) {
                
                    _logger?.LogError(error);
                }
                else {
                    
                    throw new Exception(error, ex);
                }

                command.State = CommandState.Failed;
            }

            return command;
        }

        public int CommnadQueueCount => _commandQueue.Count;
        
        public override bool CommandPending => 
                    _commandQueue.CommandPending;

        public bool ActiveOrPendingCommand => 
                    CommandPending || CurrentCommand != null;


        public CommandBase? PeekNextCommand()
        {
            if ((_commandQueue.Count != 0) &&
                        _commandQueue.TryPeek(out CommandBase? command)) {
                return command;
            }
            return null;
        }

        public bool DequeueNextCommand()
        {
            if (_commandQueue.Count > 0) {

                if (_commandQueue.Pop(out CommandBase command)) {
                    CurrentCommand = command;
                    return true;
                }
            }
            return false;
        }

        public void PurgeCommandQueue(CommandState cmdSate = CommandState.Ignored)
        {
            while (!_commandQueue.IsEmpty) {
                _commandQueue.Pop(out CommandBase command);
                command.State = cmdSate;
            }
        }

        // Interface...

        #region Interface

   

        #endregion Interface
    }
}
