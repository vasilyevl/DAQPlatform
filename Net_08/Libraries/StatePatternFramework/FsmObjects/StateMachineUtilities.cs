using Grumpy.DAQFramework.Common;

namespace Grumpy.StatePatternFramework.FsmObjects
{
    public static class StateMachineUtilities
    {
        static readonly Dictionary<Results, StateResult>
            _ioResultToStateStatus =
                new Dictionary<Results, StateResult>()
        {
            { Results.Success, StateResult.Completed },
            { Results.Error, StateResult.Error },
            { Results.Cancelled, StateResult.Completed },
            { Results.Warning, StateResult.Completed }
        };

        public static StateResult FromIOResult(Results result) {

            if (_ioResultToStateStatus.ContainsKey(result)) {
                return _ioResultToStateStatus[result];
            }
            else {
                return StateResult.NotAvailable;
            }
        }

        private static readonly Dictionary<CommandState, StateResult>
        _commandStatusToStateStatus =
            new Dictionary<CommandState, StateResult>()
{
                {CommandState.Success, StateResult.Completed},
                {CommandState.Ignored, StateResult.Completed},
                {CommandState.Rejected, StateResult.Completed},
                {CommandState.Failed, StateResult.Error},
                {CommandState.Timeout, StateResult.Error},
            };

        public static StateResult FromCommandStatus(CommandState status) {
            if (_commandStatusToStateStatus.ContainsKey(status)) {
                return _commandStatusToStateStatus[status];
            }
            else {
                return StateResult.NotAvailable;
            }
        }
    }
}
