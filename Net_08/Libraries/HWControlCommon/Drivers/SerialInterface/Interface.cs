using Grumpy.DAQFramework.Drivers;
using Grumpy.DaqFramework.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.DaqFramework.Drivers.SerialInterface
{
    public interface ISerialInterface: IGenericDeviceDriver
    {
        bool SetPortConfiguration(SerialPortConfiguration config);
        bool Write(string message);
        int Read(out string message, int maxLen, int minLen);
        int Query(string message, out string response, int maxLen, int minLen);

        // Serial Port Only 
        bool FlushRxBuffer();
        bool FlushTxBuffer();
        bool FlushBuffers();
        PortState State { get; }
        bool PortIsOpen { get; }
        bool InErrorState { get; }
        bool HasBytesToRead { get; }
        int BytesToRead { get; }
    }
}
