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

        protected const int _NoTimeoutMs = -1;
        protected const int _MinTimeoutMs = 25;


        protected CommandState _currentState;
        protected DateTime _timeout;
        protected int _timeoutMs;
        protected int _processingTimeoutMs; 
        private static long _commandID = 0;

        protected object _stateLock;

        System.Timers.Timer? _timer;

        public CommandBase( CommandTypeBase commandType,
                object? arguments,
                EventHandler? callback,
                int timeoutMs = _NoTimeoutMs,
                int processingTimeoutMs = _NoTimeoutMs) {

            CommandID = ++_commandID;

            _stateLock = new object();

            CommandType = commandType is null ? 
                CommandTypeBase.Generic : commandType;

            if (callback != null) {

                StateChanged += callback;
            }

            _currentState = CommandState.Created;

            Arguments = arguments;

            _timeoutMs = timeoutMs > 0 ? 
                    Math.Max(_MinTimeoutMs, timeoutMs) : 
                    _NoTimeoutMs;
            
            _processingTimeoutMs = processingTimeoutMs > 0 ? 
                    Math.Max(_MinTimeoutMs, processingTimeoutMs) : 
                    _NoTimeoutMs;

            _timeout = _timeoutMs > 0 ? 
                DateTime.Now + TimeSpan.FromMilliseconds(_timeoutMs) :
                DateTime.MaxValue;

            if (_timeoutMs > 0) {
                _timer = new System.Timers.Timer(_timeoutMs);
                _timer.Elapsed += _OnTimedEvent!;
                _timer.AutoReset = false;
                _timer.Enabled = true;
            }
            else {
               
                _timer = null;
            }
        }

        public void Dispose() {

            try {
                _timer?.Stop();
                _timer?.Dispose();
            }
            catch (Exception) {
                // Ignore
            }
        }

        public long CommandID { get; private set; }

        public CommandTypeBase CommandType { get; private set; }

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
                lock (_stateLock) {

                    return _currentState;
                }
            }
            set {

                lock (_stateLock) {

                    if (value != _currentState 
                        && _currentState != CommandState.Timeout) {

                        _currentState = value;

                        if ((value & (CommandState.Rejected |
                            CommandState.Success | CommandState.Error |
                            CommandState.Processed | CommandState.Processing)) != 0) {

                            _timer?.Stop();
                            _timer?.Dispose();
                        }

                        if (value == CommandState.Processing && _processingTimeoutMs > 0) {

                            _timer = new System.Timers.Timer(_processingTimeoutMs);
                            _timer.Elapsed += _OnTimedEvent!;
                            _timer.AutoReset = false;
                            _timer.Enabled = true;
                            _timer.Start();
                        }

                        _OnStatusChanged();
                    }
                }
            }
        }

        protected void _OnTimedEvent(Object source, ElapsedEventArgs e) {
            
            lock (_stateLock) {         

                if ((_currentState & CommandState.Processed) != 0) {

                    _currentState = CommandState.Timeout;
                }
            }

            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }

        protected virtual void _OnStatusChanged(object? arguments = null) {
            EventHandler? handler = StateChanged;

            if ((handler != null) && (State != CommandState.Created)) {

                var args = new CommandStateChangedEventArgs(status: State, 
                    commandType: CommandType,
                    arguments: arguments);

                handler.Invoke(this, args);
            }
        }

        ~CommandBase() => Dispose();


    }
}
