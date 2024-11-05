using Tau.ControlBase;

using System;


namespace FSM
{
    public class CommandQueue<TCommand, TCommandType> : 
            Fifo<TCommand> where TCommand : UserCommandBase<TCommandType> 
    {
        public const int MaxQueueLength = 128;

        public CommandQueue(int capacity = MaxQueueLength) : base(capacity)
        {
            MaxDepth = capacity;
        }

        public event EventHandler CommandAdded;

        public bool CommandPending => Count > 0;

        public override bool Push(TCommand cmd, bool force = false)
        {
            if ( (MaxDepth < 0) || (Count < MaxDepth))  {

                try {

                    if (base.Push(cmd, force))  {
                      
                        cmd.State = CommandState.Pending;
                        if (CommandAdded != null) {

                            var eventListeners = CommandAdded.GetInvocationList();

                            foreach (var listener in eventListeners) {

                                var methodToInvoke = listener as EventHandler;

                                if (methodToInvoke != null) {

                                    methodToInvoke.BeginInvoke(this, EventArgs.Empty, EndAsyncEvent, null);
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
                        new CommandQueueEnueueException($"Exception while adding command {cmd.Command}", e);
                    OnCommandAdded(cmd.Command, cmd.State, ex);
                    return false;
                }
            }
            else {

                cmd.State = CommandState.Ignored;
                CommandQueueEnueueException ex = 
                    new CommandQueueEnueueException($"Exception while adding command {cmd.Command}. " +
                    $"Queue at capacity {MaxDepth}");
                OnCommandAdded(cmd.Command, cmd.State, ex);
                return false;
            }
        }

        protected virtual void OnCommandAdded(TCommandType type , CommandState state, Exception e = null)
        {
            if( CommandAdded != null) {

                var eventListeners = CommandAdded.GetInvocationList();

                var args = new CommandAddedEventArgs<TCommandType>(type, state, e);
                foreach (var listener in eventListeners) {

                    var methodToInvoke = listener as EventHandler;

                    if(methodToInvoke != null) {

                        methodToInvoke.BeginInvoke(this, args, EndAsyncEvent, null);
                    }                   
                }
            }
        }

        private void EndAsyncEvent(IAsyncResult iar)
        {
            var ar = (System.Runtime.Remoting.Messaging.AsyncResult)iar;
            var invokedMethod = (EventHandler)ar.AsyncDelegate;

            try {
                invokedMethod.EndInvoke(iar);
            }
            catch {
                // Handle any exceptions that were thrown by the invoked method
               // Console.WriteLine("An event listener went kaboom!");
            }
        }
    }



    public class CommandAddedEventArgs<TCommandType> : EventArgs
    {
        public CommandAddedEventArgs(TCommandType type, CommandState state, Exception e = null):base()
        {
           Type = type;
            State = state;
        }

        public TCommandType Type
        { 
            get; 
            private set; 
        }

        public CommandState State
        {
            get;
            private set;
        }



    }

    public class CommandQueueException : Exception
    {
        public CommandQueueException(string description) : base(description)
        { }
        public CommandQueueException(string description, Exception e) : base(description, e)
        { }
    }
    public class CommandQueueEnueueException : CommandQueueException
    {
        public CommandQueueEnueueException(string description) : base(description)
        { }

        public CommandQueueEnueueException(string description, Exception e) : base(description, e)
        { }

    }

}
