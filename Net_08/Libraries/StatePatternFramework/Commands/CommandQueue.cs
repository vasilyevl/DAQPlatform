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

namespace Grumpy.StatePatternFramework
{
    public class CommandQueueEnueueException : Exception
    {
        public CommandQueueEnueueException(string message, 
            Exception? 
                innerException = null) : 
                base(message, innerException) { }
    }

    public class CommandQueue: BasicQueue<CommandBase>
    {
        private const int DeafaultQueueLength = 127;

        public CommandQueue(int capacity = DeafaultQueueLength): base(capacity) {

            Depth = capacity;
        }

        public event EventHandler? CommandAdded;

        public bool CommandPending => Count > 0;

        public override bool Push(CommandBase cmd, bool force = false) {
     
            if ((Depth < 0) || (Count < Depth)) {

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
                            $"{cmd.Type}", e);

                    OnCommandAdded(cmd, cmd.State, ex);
                    
                    return false;
                }
            }
            else {

                cmd.State = CommandState.Ignored;

                CommandQueueEnueueException ex =
                    new CommandQueueEnueueException(
                        $"Exception while adding command " +
                        $"{cmd.Type}. " +
                        $"Queue at capacity {Depth}");

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
                    new CommandAddedEventArgs(cmd.Type, state, e);

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
