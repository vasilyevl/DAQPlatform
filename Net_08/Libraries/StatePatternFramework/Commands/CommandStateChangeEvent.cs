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


namespace Grumpy.StatePatternFramework
{

    public class CommandStateChangedEventArgs : EventArgs
    {
        public CommandStateChangedEventArgs() : base() {

            NewState = CommandState.Created;
            CommandType = CommandTypeBase.Generic;
        }

        public CommandStateChangedEventArgs(CommandState status,
                        CommandTypeBase commandType,
                        object? arguments = null) : base() {

            NewState = status;
            Arguments = arguments;
            CommandType = commandType;
        }

        public CommandStateChangedEventArgs(ICommandState cmd) : base() {

            NewState = cmd.State;
            CommandType = cmd.Type;
        }

        public CommandState NewState { get; private set; }

        public CommandTypeBase CommandType { get; private set; }

        public object? Arguments { get; private set; }

        public bool Success => (NewState & CommandState.Success) != 0;

        public bool Pending => (NewState & CommandState.Pending) != 0;

        public bool IsActive => (NewState & CommandState.Active) != 0;

        public bool Processing =>
                    (NewState & CommandState.Processing) != 0;

        public bool ProcessingComplete =>
                    (NewState & CommandState.ProcessingComplete) != 0;

        public bool ProcessedCleanly =>
                    (NewState & CommandState.Processed) != 0;

        public bool Error => (NewState & CommandState.Error) != 0;

        public bool Timeout => (NewState & CommandState.Timeout) != 0;

        public bool Wrong => (NewState & CommandState.Wrong) != 0;

        public bool Failed => (NewState & CommandState.Failed) != 0;

        public bool Rejected => (NewState & CommandState.Rejected) != 0;

        public bool NA => (NewState == CommandState.NA);

        public bool Ignored => (NewState & CommandState.Ignored) != 0;

        public bool AnyFailure => Error || Failed || Timeout ||
                                  Rejected || Wrong || NA;

    }

}
