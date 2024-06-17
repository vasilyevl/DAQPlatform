using System;

namespace PissedEngineer.HWControl.Handlers
{
    public interface IEthernetConnectionConfiguration
    {
        Int32 DataPort { get; set; }
        String IpAddress { get; set; }
        Int32 MessagePort { get; set; }
        String Name { get; set; }
        Int32 Port { get; set; }
        Int32 Timeout { get; set; }
        void SetToDefaults();
        object Clone();
        bool CopyFrom(object src);
        bool CopyFrom(IEthernetConnectionConfiguration src);
    }
}
