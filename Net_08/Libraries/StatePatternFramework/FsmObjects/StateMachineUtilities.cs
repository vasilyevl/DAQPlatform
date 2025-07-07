using Grumpy.SDAQFramework.Common;

namespace Grumpy.StatePatternFramework.FsmObjects
{
    public static class StateMachineUtilities
    {
        static readonly Dictionary<Results, StateResult>
            _ioResultToStateStatus =
                new Dictionary<Results, StateResult>()
        {
            { Results.Success, StateResult.Success },
            { Results.Error, StateResult.Error },
            { Results.Cancelled, StateResult.Success },
            { Results.Warning, StateResult.Success }
        };

        public static StateResult FromIOResult(Results result) {

            if (_ioResultToStateStatus.ContainsKey(result)) {
                return _ioResultToStateStatus[result];
            }
            else {
                return StateResult.NA;
            }
        }

        private static readonly Dictionary<CommandState, StateResult>
        _commandStatusToStateStatus =
            new Dictionary<CommandState, StateResult>()
{
                {CommandState.Success, StateResult.Success},
                {CommandState.Ignored, StateResult.Success},
                {CommandState.Rejected, StateResult.Success},
                {CommandState.Failed, StateResult.Error},
                {CommandState.Timeout, StateResult.Error},
            };

        public static StateResult FromCommandStatus(CommandState status) {
            if (_commandStatusToStateStatus.ContainsKey(status)) {
                return _commandStatusToStateStatus[status];
            }
            else {
                return StateResult.NA;
            }
        }
    }
}
