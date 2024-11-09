using Grumpy.DAQFramework.Common;

namespace Grumpy.StatePatternFramework.FsmObjects
{
    public static class StateMachineUtilities
    {
        static readonly Dictionary<IOResults, StateExecutionResult>
            _ioResultToStateStatus =
                new Dictionary<IOResults, StateExecutionResult>()
        {
            { IOResults.Success, StateExecutionResult.Completed },
            { IOResults.Error, StateExecutionResult.Error },
            { IOResults.Cancelled, StateExecutionResult.Completed },
            { IOResults.Warning, StateExecutionResult.Completed }
        };

        public static StateExecutionResult FromIOResult(IOResults result) {

            if (_ioResultToStateStatus.ContainsKey(result)) {
                return _ioResultToStateStatus[result];
            }
            else {
                return StateExecutionResult.NotAvailable;
            }
        }

        private static readonly Dictionary<CommandState, StateExecutionResult>
        _commandStatusToStateStatus =
            new Dictionary<CommandState, StateExecutionResult>()
{
                {CommandState.Success, StateExecutionResult.Completed},
                {CommandState.Ignored, StateExecutionResult.Completed},
                {CommandState.Rejected, StateExecutionResult.Completed},
                {CommandState.Failed, StateExecutionResult.Error},
                {CommandState.Timeout, StateExecutionResult.Error},
            };

        public static StateExecutionResult FromCommandStatus(CommandState status) {
            if (_commandStatusToStateStatus.ContainsKey(status)) {
                return _commandStatusToStateStatus[status];
            }
            else {
                return StateExecutionResult.NotAvailable;
            }
        }
    }
}
