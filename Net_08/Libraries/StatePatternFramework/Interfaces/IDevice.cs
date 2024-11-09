

namespace Grumpy.StatePatternFramework
{
    public interface IDevice<TConfiguration, TCommandType, THwHandler>
    {
        string Name { get; }
        bool SetConfiguration(TConfiguration configuration);
        bool IsOpen { get; }
        CommandState Open(EventHandler? callback = null);
        bool Close();
        int EnqueueCommand(CommandBase command);
        CommandBase PeekNextCommand();
        CommandBase DequeueNextCommand();
        THwHandler Handler { get; }
    }
}
