using DAQFramework.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.DAQFramework.Drivers
{
    public interface IGenericDeviceDriver
    {
        public bool Open();
        public bool Close();
        public bool Reset();
        public void ClearError();
        public bool IsOpen { get; }
        string LastError { get; }
        IOResults LastIOResult { get; }
    }
}
