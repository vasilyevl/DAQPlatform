
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
            CommandType = cmd.CommandType;
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
