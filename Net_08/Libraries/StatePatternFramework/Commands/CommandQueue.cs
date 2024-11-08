using Grumpy.DaqFramework.Common;

namespace Grumpy.StatePatternFramework
{
    public class CommandQueueEnueueException : Exception
    {
        public CommandQueueEnueueException(string message, 
            Exception? 
                innerException = null) : 
                base(message, innerException) { }
    }

    public class CommandQueue:BasicQueue<CommandBase>
    {
        public const int MaxQueueLength = 127;

        public CommandQueue(int capacity = MaxQueueLength): base(capacity) {

            MaxDepth = capacity;
        }

        public event EventHandler? CommandAdded;

        public bool CommandPending => Count > 0;

        public override bool Push(CommandBase cmd, bool force = false) {
     
            if ((MaxDepth < 0) || (Count < MaxDepth)) {

                try {

                    if (base.Push(cmd, force)) {

                        cmd.State = CommandState.Pending;

                        if (CommandAdded != null) {

                            Delegate[] eventListeners = 
                                CommandAdded.GetInvocationList();

                            foreach (var listener in eventListeners) {

                                var methodToInvoke = listener as EventHandler;

                                if (methodToInvoke != null) {

                                    Task.Factory.StartNew(() => 
                                        methodToInvoke.Invoke(this, 
                                                        EventArgs.Empty));
                                }
                            }
                        }
            
                        return true;
                    }
                    
                    return false;
                }
                catch (Exception e) {

                    cmd.State = CommandState.Ignored;

                    CommandQueueEnueueException ex =
                        new CommandQueueEnueueException(
                            $"Exception while adding command " +
                            $"{cmd.CommandType}", e);

                    OnCommandAdded(cmd, cmd.State, ex);
                    
                    return false;
                }
            }
            else {

                cmd.State = CommandState.Ignored;

                CommandQueueEnueueException ex =
                    new CommandQueueEnueueException(
                        $"Exception while adding command " +
                        $"{cmd.CommandType}. " +
                        $"Queue at capacity {MaxDepth}");

                OnCommandAdded(cmd, cmd.State, ex);

                return false;
            }
        }

        protected virtual void OnCommandAdded(CommandBase cmd, 
                                              CommandState state, 
                                              Exception? e = null) 
        {
            if (CommandAdded != null) {

                var eventListeners = CommandAdded.GetInvocationList();

                var args = 
                    new CommandAddedEventArgs(cmd.CommandType, state, e);

                foreach (var listener in eventListeners) {

                    var methodToInvoke = listener as EventHandler;

                    if (methodToInvoke != null) {

                        Task.Factory.StartNew(() => 
                            methodToInvoke.Invoke(this, args),
                            TaskCreationOptions.LongRunning);
                    }
                }
            }
        }
    }

    public class CommandAddedEventArgs : EventArgs
    {
        public CommandAddedEventArgs(CommandTypeBase type, 
                                     CommandState state, 
                                     Exception? e = null) : base() {
            Type = type;
            State = state;
        }

        public CommandTypeBase Type {
            get;
            private set;
        }

        public CommandState State {
            get;
            private set;
        }
    }
}
