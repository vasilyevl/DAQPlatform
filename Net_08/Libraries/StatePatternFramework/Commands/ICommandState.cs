using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.StatePatternFramework
{
    public interface ICommandState
    {
        long CommandID { get; }
        CommandTypeBase CommandType { get; }
        CommandState State { get; }
        bool HasArguments { get; }
        bool ProcessingComplete { get; }
        bool InProcessing { get; }
        bool Pening { get; }
        bool Success { get; }
        bool Failed { get; }
        bool TimedOut { get; }
    }

}
