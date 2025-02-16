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

    /// <summary>
    /// Provides data for the state change event.
    /// </summary>
    public class StateChangeEventArgs : EventArgs
    {

        private EnumBase? _newState;
        private EnumBase? _previousState;

        public StateChangeEventArgs(EnumBase newState,
                                     EnumBase? previousState = null) {
            _previousState = previousState;
            _newState = newState;
        }

        /// <summary>
        /// Gets the previous state.
        /// </summary>
        public EnumBase? PreviousState => _previousState;

        /// <summary>
        /// Gets the new state.
        /// </summary>
        public EnumBase? NewState => _newState;
    }

    /// <summary>
    /// Provides data for the state processing event.
    /// </summary>
    public class StateProcArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateProcArgs"/> class.
        /// </summary>
        public StateProcArgs() : base() {
            IdlingExitTrigger = IdlingExitTrigger.NA;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateProcArgs"/> class with the specified idling result.
        /// </summary>
        /// <param name="st">The idling result.</param>
        public StateProcArgs(IdlingExitTrigger st) : base() {
            IdlingExitTrigger = st;
        }

        /// <summary>
        /// Gets or sets the idling result.
        /// </summary>
        public IdlingExitTrigger IdlingExitTrigger { get; set; }
    }

    /// <summary>
    /// Represents the method that will handle the state change event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="StateChangeEventArgs"/> that contains the event data.</param>
    public delegate void StateChangeEventHandler(object sender,
                                                 StateChangeEventArgs e);
}


    