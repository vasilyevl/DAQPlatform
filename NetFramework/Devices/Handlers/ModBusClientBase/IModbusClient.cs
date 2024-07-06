/*
This code is based on  2018-2020 Rossmann-Engineering EasyModbus project. 
The original code is available at:
https://github.com/rossmann-engineering/EasyModbusTCP.NET
  
Copyright (c) 2024 LV-PissedEngineer Permission is hereby granted, 
free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"),to deal in the Software 
without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the 
Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.IO.Ports;

namespace ModeBusHandler
{
    public interface IModbusClient
    {
        int Baudrate { get; set; }
        bool Connected { get; }
        int ConnectionTimeout { get; set; }
        String IPAddress { get; set; }
        bool IsConnected { get; }
        String LastError { get; }
        int NumberOfRetries { get; set; }
        Parity Parity { get; set; }
        int Port { get; set; }
        String SerialPort { get; set; }
        StopBits StopBits { get; set; }
        bool UDPFlag { get; set; }
        Byte UnitIdentifier { get; set; }

        event ModbusClient.ConnectedChangedHandler ConnectedChanged;
        event ModbusClient.ReceiveDataChangedHandler ReceiveDataChanged;
        event ModbusClient.SendDataChangedHandler SendDataChanged;

        bool Available(int timeout);
        bool Connect();
        bool ConvertRegistersToDouble(int[] registers, out Double result);
        bool ConvertRegistersToDouble(int[] registers, ModbusClient.RegisterOrder registerOrder, out Double result);
        bool ConvertRegistersToFloat(int[] registers, out Single result);
        bool ConvertRegistersToFloat(int[] registers, ModbusClient.RegisterOrder registerOrder, out Single result);
        bool ConvertRegistersToInt(int[] registers, out int result);
        bool ConvertRegistersToInt(int[] registers, ModbusClient.RegisterOrder registerOrder, out int result);
        bool ConvertRegistersToLong(int[] registers, out Int64 result);
        bool ConvertRegistersToLong(int[] registers, ModbusClient.RegisterOrder registerOrder, out Int64 result);
        void Disconnect();
        bool ReadCoils(int startingAddress, int quantity, out bool[] coils, int functionCode = 1);
        bool ReadDiscreteInputs(int startingAddress, int quantity, out bool[] response, int functionCode = 2);
        bool ReadHoldingRegisters(int startingAddress, int quantity, out int[] registers, int functionCode = 3);
        bool ReadInputRegisters(int startingAddress, int quantity, out int[] response, int functionCode = 3);
        bool ReadSingle16bitRegister(int address, out UInt16 value, int functionCode = 3);
        bool ReadSingle32bitRegister(int address, out int value, int functionCode = 3);
        int[] ReadWriteMultipleRegisters(int startingAddressRead, int quantityRead, int startingAddressWrite, int[] values, int functionCode = 23);
        bool WriteMultipleCoils(int startingAddress, bool[] values, int functionCode = 15);
        bool WriteMultipleRegisters(int startingAddress, int[] values, int functionCode = 16);
        bool WriteSingle16bitRegister(int startingAddress, UInt16 value, int functionCode = 6);
        bool WriteSingle32bitRegister(int startingAddress, int value, int functionCode = 6);
        bool WriteSingleCoil(int startingAddress, bool value, int functionCode = 5);
    }
}