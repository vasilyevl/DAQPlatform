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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grumpy.DAQFramework.Configuration
{
    public enum IOStatus
    {
        Unknown = 0,
        Success = 1,
        Error = 2,
        Warning = 4,
        Cancelled = 8,
        Ignored = 16,
        Pending = 32,
        OK =Success | Ignored,
        NoError =  OK | Cancelled | Warning,
        Complete = OK | Error | Cancelled | Warning
    }


    public class IoStatus : IEquatable<IoStatus>, IEquatable<IOStatus>
    {
        private IOStatus _ioResult;
        private object _ioStateLock;
        private long _instance;
        protected static long _instanceCounter = 0;

        public IoStatus()
        {
            _ioResult = IOStatus.Unknown;
            _ioStateLock = new object();
            _instance = Interlocked.Increment(ref _instanceCounter);
        }

        public IoStatus(IOStatus initialState) : this()
        {
            _ioResult = initialState;
        }

        public EventHandler<IoStateChagedEventArgs>? OnStateChanged;

        public class IoStateChagedEventArgs : EventArgs
        {
            public IoStateChagedEventArgs(IOStatus prevSt, IOStatus newSt)
            {
                PreviousState = new IoStatus(prevSt);
                NewState = new IoStatus(newSt);
            }

            public IoStatus PreviousState { get; private set; }
            public IoStatus NewState { get; private set; }
        }



        public IOStatus State {
            get {
                lock (_ioStateLock) {

                    return _ioResult;
                }
            }

            set {
                IOStatus prevSt = IOStatus.Unknown;
                IOStatus newSt =  IOStatus.Unknown;

                lock (_ioStateLock) {

                    if (_ioResult != value) {

                        prevSt = _ioResult;
                        _ioResult = value;
                        newSt = _ioResult;
                    }
                }

                OnStateChangedEvent(prevSt, newSt);
            }
        }

        public bool Add(IOStatus state)
        {

            IOStatus prevSt = IOStatus.Unknown;
            IOStatus newSt =  IOStatus.Unknown;

            bool r = false;
            lock (_ioStateLock) {

                if (state != IOStatus.Unknown && (_ioResult & state) == 0) {
                    prevSt = _ioResult;
                    _ioResult |= state;
                    newSt = _ioResult;
                    r = true;
                }
            }

            if ((prevSt & newSt) != 0) {

                OnStateChangedEvent(prevSt, newSt);
            }

            return r;
        }


        private void OnStateChangedEvent(IOStatus prevSt, IOStatus newSt)
        {
            if ((OnStateChanged?.GetInvocationList().Count() ?? 0) > 0) {

                OnStateChanged?.Invoke(this, new IoStateChagedEventArgs(prevSt, newSt));
            }
        }

        public bool IsSuccess => State == IOStatus.Success;

        public bool IsError => (State & IOStatus.Error) != 0;

        public bool IsCompleteOK => (State & IOStatus.Complete) != 0;

        public bool IsComplete => (State & IOStatus.Complete) != 0;

        public bool IsCancelled => (State & IOStatus.Cancelled) != 0;

        public bool IsPending => (State & IOStatus.Pending) != 0;

        public bool Ignored => (State & IOStatus.Ignored) != 0;

        public override bool Equals(object? obj)
        {
            return (obj is IoStatus) || (obj is IOStatus) ? Equals(obj) : false;
        }

        public bool Equals(IoStatus? other)
        {
            lock (_ioStateLock) {
                return (other! == null!) ? false: _ioResult == other.State;
            }
        }

        public bool Equals(IOStatus other)
        {
            lock (_ioStateLock) {
                return _ioResult == other;
            }
        }

        public static bool operator ==(IoStatus left, IoStatus right)
        {
            return left?.Equals(right) ?? false;
        }

        public static bool operator !=(IoStatus left, IoStatus right)
        {
            return left?.Equals(right) ?? false;
        }

        public static bool operator ==(IoStatus left, IOStatus right)
        {
            return left?.Equals(right) ?? false;
        }

        public static bool operator !=(IoStatus left, IOStatus right)
        {
            return left?.Equals(right) ?? false;
        }

        public static bool operator ==(IOStatus left, IoStatus right)
        {
            return right?.Equals(left) ?? false;
        }

        public static bool operator !=(IOStatus left, IoStatus right)
        {
            return right?.Equals(right) ?? false;
        }

        public override int GetHashCode()
        {
            return _ioResult.GetHashCode() + GetType().GetHashCode() + _instance.GetHashCode();
        }

    }
  
}
