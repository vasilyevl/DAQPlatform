using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.StatePatternFramework
{
    public enum StateExecutionResult
    {
        Completed = 0,
        Error = 1,
        Timeout = 2,
        Idling = 4,
        Working = 8,
        NotAvailable = 16
    }
}
