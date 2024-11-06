using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAQFramework.Common
{
    [Flags]
    public enum IOResults
    {
        NA = 0,
        Success = 1,
        Error = 2,
        Timeout = 4,
        NotSupported = 8,
        NotReady = 16,
        NotAvailable = 32,
        NotApplicable = 64,
        Cancelled = 128,
        Warning = 256,
        NotImplemented = 512,
        Ignored = 1024
    }
}
