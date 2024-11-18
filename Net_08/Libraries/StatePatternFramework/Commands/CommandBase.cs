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

using System.Timers;

namespace Grumpy.StatePatternFramework
{
    [Flags]
    public enum CommandState
    {
        NA = 0,
        Created = 1,
        Pending = 2,
        Processing = 4,
        Success = 8,
        Failed = 16,
        Timeout = 32,
        Ignored = 64,
        Wrong = 128,
        Rejected = 256,

        Active = Pending | Processing,
        ProcessingComplete = Success | Failed | Timeout | 
                             Ignored | Wrong | Rejected,
        Error = Failed | Timeout | Wrong,
        Processed = Success | Failed | Ignored | Rejected
    }

    public class CommandBase : ICommandState, IDisposable
    {
        public event EventHandler? StateChanged;

        protected const int NoTimeoutMs = -1;
        protected const int MinTimeoutMs = 25;


        protected CommandState currentState;
        protected DateTime timeout;
        protected int timeoutMs;
        protected int processingTimeoutMs; 
        private static long id = 0;

        protected object stateLock;

        System.Timers.Timer? timer;

        public CommandBase() {

            stateLock = new object();
            ID = ++id;
            Type = CommandTypeBase.Generic;
            currentState = CommandState.Created;
            timeoutMs = NoTimeoutMs;
            processingTimeoutMs = NoTimeoutMs;
            timeout = DateTime.MaxValue;
            Arguments = null;
            timer = null;
        }


        public CommandBase( CommandTypeBase commandType,
                object? arguments,
                EventHandler? callback,
                int timeoutMs = NoTimeoutMs,
                int processingTimeoutMs = NoTimeoutMs): this() {

            Type = commandType;

            if (callback != null) {

                StateChanged += callback;
            }

            Arguments = arguments;

            this.timeoutMs = timeoutMs > 0 ?
                    Math.Max(MinTimeoutMs, timeoutMs) :
                    NoTimeoutMs;
            
            this.processingTimeoutMs = processingTimeoutMs > 0 ?
                    Math.Max(MinTimeoutMs, processingTimeoutMs) :
                    NoTimeoutMs;

            timeout = this.timeoutMs > 0 ?
                DateTime.Now + TimeSpan.FromMilliseconds(this.timeoutMs) :
                DateTime.MaxValue;

            if (this.timeoutMs > 0) {
                timer = new System.Timers.Timer(this.timeoutMs);
                timer.Elapsed += _OnTimedEvent!;
                timer.AutoReset = false;
                timer.Enabled = true;
            }
            else {

                timer = null;
            }
        }

        public void Dispose() {

            try {
                timer?.Stop();
                timer?.Dispose();
            }
            catch (Exception) {
                // Ignore
            }
        }

        public long ID { get; private set; }

        public CommandTypeBase Type { get; private set; }

        public object? Arguments { get; private set; }

        public bool HasArguments => Arguments != null ;

        public bool Success => (State & CommandState.Success) != 0;
        public virtual bool ProcessingComplete => 
                    (State & CommandState.ProcessingComplete) != 0;

        public bool InProcessing => 
                    (State & CommandState.Processing) != 0;

        public bool Pening => (State & CommandState.Pending) != 0;

        public bool Failed => (State & CommandState.Failed) != 0;

        public bool TimedOut => (State & CommandState.Timeout) != 0;

        public bool WrongCommand => (State & CommandState.Wrong) != 0;

        public virtual CommandState State {
            get {
                lock (stateLock) {

                    return currentState;
                }
            }
            set {

                lock (stateLock) {

                    if (value != currentState 
                        && currentState != CommandState.Timeout) {

                        currentState = value;

                        if ((value & (CommandState.Rejected 
                            | CommandState.Success 
                            | CommandState.Error 
                            | CommandState.Processed 
                            | CommandState.Processing)) != 0) {

                            timer?.Stop();
                            timer?.Dispose();
                        }

                        if ( value == CommandState.Processing 
                            && processingTimeoutMs > 0 ) {

                            timer = new System.Timers.Timer(processingTimeoutMs);
                            timer.Elapsed += _OnTimedEvent!;
                            timer.AutoReset = false;
                            timer.Enabled = true;
                            timer.Start();
                        }

                        _OnStatusChanged();
                    }
                }
            }
        }

        protected void _OnTimedEvent(Object source, ElapsedEventArgs e) {
            
            lock (stateLock) {         

                if ((currentState & CommandState.Processed) != 0) {

                    currentState = CommandState.Timeout;
                }
            }

            timer?.Stop();
            timer?.Dispose();
            timer = null;
        }

        protected virtual void _OnStatusChanged(object? arguments = null) {
            EventHandler? handler = StateChanged;

            if ((handler != null) && (State != CommandState.Created)) {

                var args = new CommandStateChangedEventArgs(status: State, 
                    commandType: Type,
                    arguments: arguments);

                handler.Invoke(this, args);
            }
        }

        ~CommandBase() => Dispose();
    }
}
