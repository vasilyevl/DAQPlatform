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


namespace Grumpy.SDAQFramework.Configuration
{
    /// <summary>
    /// Represents the possible I/O operation statuses for serial port or device communication.
    /// </summary>
    public enum IOStatus
    {
        /// <summary>State is unknown or not set.</summary>
        Unknown = 0,
        /// <summary>Operation completed successfully.</summary>
        Success = 1,
        /// <summary>An error occurred during the operation.</summary>
        Error = 2,
        /// <summary>Operation completed with a warning.</summary>
        Warning = 4,
        /// <summary>Operation was cancelled.</summary>
        Cancelled = 8,
        /// <summary>Operation was ignored.</summary>
        Ignored = 16,
        /// <summary>Operation is pending.</summary>
        Pending = 32,
        /// <summary>Operation completed successfully or was ignored.</summary>
        OK = Success | Ignored,
        /// <summary>No error occurred (OK, Cancelled, or Warning).</summary>
        NoError =  OK | Cancelled | Warning,
        /// <summary>Operation is complete (OK, Error, Cancelled, or Warning).</summary>
        Complete = OK | Error | Cancelled | Warning
    }

    /// <summary>
    /// Provides a thread-safe, observable wrapper for <see cref="IOStatus"/> with state change notification and comparison logic.
    /// </summary>
    public class IoStatus : IEquatable<IoStatus>, IEquatable<IOStatus>
    {
        private IOStatus _ioResult;
        private object _ioStateLock;
        private long _instance;
        /// <summary>
        /// Global instance counter for IoStatus objects.
        /// </summary>
        protected static long _instanceCounter = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="IoStatus"/> class with state <see cref="IOStatus.Unknown"/>.
        /// </summary>
        public IoStatus()
        {
            _ioResult = IOStatus.Unknown;
            _ioStateLock = new object();
            _instance = Interlocked.Increment(ref _instanceCounter);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IoStatus"/> class with a specified initial state.
        /// </summary>
        /// <param name="initialState">The initial <see cref="IOStatus"/> value.</param>
        public IoStatus(IOStatus initialState) : this()
        {
            _ioResult = initialState;
        }

        /// <summary>
        /// Occurs when the state changes.
        /// </summary>
        public EventHandler<IoStateChagedEventArgs>? OnStateChanged;

        /// <summary>
        /// Provides event data for <see cref="IoStatus.OnStateChanged"/>.
        /// </summary>
        public class IoStateChagedEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="IoStateChagedEventArgs"/> class.
            /// </summary>
            /// <param name="prevSt">The previous state.</param>
            /// <param name="newSt">The new state.</param>
            public IoStateChagedEventArgs(IOStatus prevSt, IOStatus newSt)
            {
                PreviousState = new IoStatus(prevSt);
                NewState = new IoStatus(newSt);
            }

            /// <summary>
            /// Gets the previous state.
            /// </summary>
            public IoStatus PreviousState { get; private set; }
            /// <summary>
            /// Gets the new state.
            /// </summary>
            public IoStatus NewState { get; private set; }
        }

        /// <summary>
        /// Gets or sets the current I/O status. Setting this property triggers <see cref="OnStateChanged"/> if the value changes.
        /// </summary>
        public IOStatus State
        {
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

        /// <summary>
        /// Adds a status flag to the current state if not already present.
        /// </summary>
        /// <param name="state">The <see cref="IOStatus"/> flag to add.</param>
        /// <returns>True if the state was changed; otherwise, false.</returns>
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

        /// <summary>
        /// Raises the <see cref="OnStateChanged"/> event.
        /// </summary>
        /// <param name="prevSt">The previous state.</param>
        /// <param name="newSt">The new state.</param>
        private void OnStateChangedEvent(IOStatus prevSt, IOStatus newSt)
        {
            if ((OnStateChanged?.GetInvocationList().Count() ?? 0) > 0) {
                OnStateChanged?.Invoke(this, new IoStateChagedEventArgs(prevSt, newSt));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current state is <see cref="IOStatus.Success"/>.
        /// </summary>
        public bool IsSuccess => State == IOStatus.Success;

        /// <summary>
        /// Gets a value indicating whether the current state includes <see cref="IOStatus.Error"/>.
        /// </summary>
        public bool IsError => (State & IOStatus.Error) != 0;

        /// <summary>
        /// Gets a value indicating whether the current state is complete and OK.
        /// </summary>
        public bool IsCompleteOK => (State & IOStatus.Complete) != 0;

        /// <summary>
        /// Gets a value indicating whether the current state is complete.
        /// </summary>
        public bool IsComplete => (State & IOStatus.Complete) != 0;

        /// <summary>
        /// Gets a value indicating whether the current state includes <see cref="IOStatus.Cancelled"/>.
        /// </summary>
        public bool IsCancelled => (State & IOStatus.Cancelled) != 0;

        /// <summary>
        /// Gets a value indicating whether the current state includes <see cref="IOStatus.Pending"/>.
        /// </summary>
        public bool IsPending => (State & IOStatus.Pending) != 0;

        /// <summary>
        /// Gets a value indicating whether the current state includes <see cref="IOStatus.Ignored"/>.
        /// </summary>
        public bool Ignored => (State & IOStatus.Ignored) != 0;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return (obj is IoStatus) || (obj is IOStatus) ? Equals(obj) : false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="IoStatus"/> is equal to the current instance.
        /// </summary>
        /// <param name="other">The <see cref="IoStatus"/> to compare with.</param>
        /// <returns>True if equal; otherwise, false.</returns>
        public bool Equals(IoStatus? other)
        {
            lock (_ioStateLock) {
                return (other! == null!) ? false : _ioResult == other.State;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="IOStatus"/> is equal to the current instance.
        /// </summary>
        /// <param name="other">The <see cref="IOStatus"/> to compare with.</param>
        /// <returns>True if equal; otherwise, false.</returns>
        public bool Equals(IOStatus other)
        {
            lock (_ioStateLock) {
                return _ioResult == other;
            }
        }

        /// <summary>
        /// Determines whether two <see cref="IoStatus"/> instances are equal.
        /// </summary>
        public static bool operator ==(IoStatus left, IoStatus right)
        {
            return left?.Equals(right) ?? false;
        }

        /// <summary>
        /// Determines whether two <see cref="IoStatus"/> instances are not equal.
        /// </summary>
        public static bool operator !=(IoStatus left, IoStatus right)
        {
            return left?.Equals(right) ?? false;
        }

        /// <summary>
        /// Determines whether an <see cref="IoStatus"/> and an <see cref="IOStatus"/> are equal.
        /// </summary>
        public static bool operator ==(IoStatus left, IOStatus right)
        {
            return left?.Equals(right) ?? false;
        }

        /// <summary>
        /// Determines whether an <see cref="IoStatus"/> and an <see cref="IOStatus"/> are not equal.
        /// </summary>
        public static bool operator !=(IoStatus left, IOStatus right)
        {
            return left?.Equals(right) ?? false;
        }

        /// <summary>
        /// Determines whether an <see cref="IOStatus"/> and an <see cref="IoStatus"/> are equal.
        /// </summary>
        public static bool operator ==(IOStatus left, IoStatus right)
        {
            return right?.Equals(left) ?? false;
        }

        /// <summary>
        /// Determines whether an <see cref="IOStatus"/> and an <see cref="IoStatus"/> are not equal.
        /// </summary>
        public static bool operator !=(IOStatus left, IoStatus right)
        {
            return right?.Equals(right) ?? false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _ioResult.GetHashCode() + GetType().GetHashCode() + _instance.GetHashCode();
        }
    }
}
