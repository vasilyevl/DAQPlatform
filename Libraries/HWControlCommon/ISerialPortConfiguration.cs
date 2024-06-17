using System.IO.Ports;

namespace PissedEngineer.HWControl.Handlers
{
    public interface ISerialPortConfiguration
    {
        System.Int32 BaudRate { get; set; }
        System.Int32 Bits { get; set; }
        Handshake HandShake { get; set; }
        System.Int32 MinTimeBetweenTransactionsMs { get; set; }
        System.String Name { get; set; }
        Parity Parity { get; set; }
        System.Boolean PortNameIsDefault { get; }
        System.Int32 ReadTimeoutMs { get; set; }
        System.String RxMessageTerminator { get; set; }
        StopBits StopBits { get; set; }
        System.String TxMessageTerminator { get; set; }
        System.Int32 WriteReadTimeout { get; }
        System.Int32 WriteTimeoutMs { get; set; }

        System.Boolean CopyFrom(System.Object src);
        void SetToDefaults();
        System.String ToString();
    }
}