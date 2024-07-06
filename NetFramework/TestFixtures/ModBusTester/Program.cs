using ModeBusHandler;
using PissedEngineer.HWControl.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModBusTester
{
    internal class Program
    {
        static void Main(string[] args)
        {

            ModbusClient client = new ModbusClient();

            client.IPAddress = "192.168.1.35";
            client.Port = 502;


            client.Connect();

        

            try {

                client.WriteSingleCoil(16384, true);
                client.ReadCoils(16384, 1, out bool[] input);
                client.WriteSingleCoil(16384, false);
                client.ReadCoils(16384, 1, out input);
            }
            catch (Exception ex){ 
            
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Done, Click 'Enter' to quit");

            Console.ReadLine();
        }
    }
}
