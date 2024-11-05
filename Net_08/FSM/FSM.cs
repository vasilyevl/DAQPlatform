using Tau.Common;
using Tau.Common.Configuration;
using Tau.HwControlBase;
using Tau.ControlBase;
using System;
using System.Threading;
using System.Runtime.CompilerServices;

namespace FSM
{
    public abstract class Fsm<TCommandType> : FSMBase                                             
    {
        protected int _workerCycle;

        protected CommandQueue<UserCommandBase<TCommandType>, TCommandType> _commandQueue;      

        public Fsm(string deviceName) : base(deviceName)
        {
            _commandQueue = new CommandQueue<UserCommandBase<TCommandType>, TCommandType>();
            
            CurrentCommand = null;
        }

        protected UserCommandBase<TCommandType> _currentCommand;
        public UserCommandBase<TCommandType> CurrentCommand { 
            get => _currentCommand;
            private set => _currentCommand = value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearCurrentCommand() => CurrentCommand = null;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CurrentCommandComplete(Tau.ControlBase.CommandState status)
        {
            _currentCommand.State = status;
            ClearCurrentCommand();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CurrentCommandSuccess()
        {
            _currentCommand.State = Tau.ControlBase.CommandState.Success;
            ClearCurrentCommand();
        }

        public void CurrentCommandError()
        {
            _currentCommand.State = Tau.ControlBase.CommandState.Error;
        }

        public void CurrentCommandTimeout()
        {
            _currentCommand.State = Tau.ControlBase.CommandState.Timeout;

        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UserCommandBase<TCommandType> PopCurrentCommand()
        {
            var cmd = CurrentCommand;
            ClearCurrentCommand();
            return cmd;
        }

        public ITypedCommandStatus<TCommandType> EnqueueCommand(UserCommandBase<TCommandType> command)
        {
            try {
                if (_commandQueue.Push(command)) {

                    ResumeFsmThread();

                    command.State = CommandState.Pending;
                }
                else {

                    _logger.Warning($"FSM_Device. Failed to enque command {command.Command}. " +
                        $"Command queue might be at capacity.");
                    command.State = CommandState.Ignored;
                }
            }
            catch (Exception ex) {
                _logger.Error($"FSM_Device. Failed to enque command {command.Command}. " +
                    $"Exception: {ex.Message}");
                command.State = CommandState.Failed;
            }
            return command;
        }

        public int CommnadQueueCount => _commandQueue.Count;
        
        public override bool CommandPending => _commandQueue.CommandPending;

        public bool ActiveOrPendingCommand => CommandPending || CurrentCommand != null;


        public UserCommandBase<TCommandType> PeekNextCommand()
        {
            if ((_commandQueue.Count != 0) &&
                        _commandQueue.TryPeek(out UserCommandBase<TCommandType> command)) {
                return command;
            }
            return null;
        }

        public bool DequeueNextCommand()
        {
            if (_commandQueue.Count > 0) {

                if (_commandQueue.Pop(out UserCommandBase<TCommandType> command)) {
                    CurrentCommand = command;
                    return true;
                }
            }
            return false;
        }

        public void PurgeCommandQueue(CommandState cmdSate = CommandState.Ignored)
        {
            while (!_commandQueue.IsEmpty) {
                _commandQueue.Pop(out UserCommandBase<TCommandType> command);
                command.State = cmdSate;
            }
        }

        // Interface...

        #region Interface

   

        #endregion Interface
    }
}
