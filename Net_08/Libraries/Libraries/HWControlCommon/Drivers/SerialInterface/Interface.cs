using Grumpy.HWControl.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HWControlUtilities.Drivers.SerialInterface
{
    public interface ISerialInterface
    {
        bool SetPortConfiguration(SerialPortConfiguration config);
        bool Open();
        bool Close();
        bool IsConnected { get; }
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
        string LastError { get; }
    }
}
