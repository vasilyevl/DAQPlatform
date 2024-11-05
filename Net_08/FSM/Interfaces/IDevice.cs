using Tau.ControlBase;

using System;

namespace FSM
{
    public interface IDevice<TConfiguration, TCommandType, THwHandler>
    {
        string Name { get; }
        SetValueResult SetConfiguration(TConfiguration configuration);
        bool IsOpen { get; }
        CommandState Open(EventHandler callback = null);
        bool Close();
        int EnqueueCommand(UserCommandBase<TCommandType> command);
        UserCommandBase<TCommandType> PeekNextCommand();
        UserCommandBase<TCommandType> DequeueNextCommand();
        THwHandler Handler { get; }
    }
}
