using PissedEngineer.HWControl.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PissedEngineer.HWControl
{
    public static class HwControlObjectFactory
    {
        public static IEthernetConnectionConfiguration CreateEthernetConnectionConfiguration() =>
            new EthernetConnectionConfiguration();

        public static IEthernetConnectionConfiguration CreateEthernetConnectionConfiguration(
                        string name, string ipAddress, int port) =>
                new EthernetConnectionConfiguration() {
                    Name = "ClickPLC",
                    IpAddress = "192.168.1.22",
                    Port = 502
                };

        public static IEthernetConnectionConfiguration CreateEthernetConnectionConfiguration(
            IEthernetConnectionConfiguration src) {
            return new EthernetConnectionConfiguration(src);
        }

        public static ISerialPortConfiguration CreateSerialPortConfiguration() =>
            new SerialPortConfiguration();

        public static ISerialPortConfiguration CreateSerialPortConfiguration(string portName) =>
            new SerialPortConfiguration(portName);

        public static ISerialPortConfiguration CreateSerialPortConfiguration(
            SerialPortConfiguration source, string newPortName = null) =>
            new SerialPortConfiguration(source, newPortName);
    }
}
