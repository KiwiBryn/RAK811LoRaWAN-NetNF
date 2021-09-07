//---------------------------------------------------------------------------------
// Copyright (c) June 2020, devMobile Software
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//---------------------------------------------------------------------------------
#define SERIAL_SYNC_READ
//#define SERIAL_ASYNC_READ
//#define ESP32_WROOM   //nanoff --target ESP32_WROOM_32 --serialport COM4 --update
// June 2020 experiencing issues with ComPort assignments
//#define NETDUINO3_WIFI   // nanoff --target NETDUINO3_WIFI --update
//#define MBN_QUAIL // nanoff --target MBN_QUAIL --update
// June 2020 experiencing issues with "Couldn't find a valid native assembly required by nanoFramework.System.Text v1.1.0.2, checksum 0x8E6EB73D"
//#define ST_NUCLEO64_F091RC // nanoff --target ST_NUCLEO64_F091RC --update 
//#define ST_NUCLEO144_F746ZG //nanoff --target ST_NUCLEO144_F746ZG --update
#define ST_STM32F769I_DISCOVERY      // nanoff --target ST_STM32F769I_DISCOVERY --update 
namespace devMobile.IoT.Rak811.ShieldSerial
{
   using System;
   using System.Diagnostics;
   using System.IO.Ports;
   using System.Threading;

#if ESP32_WROOM_32_LORA_1_CHANNEL
   using nanoFramework.Hardware.Esp32;
#endif

   public class Program
   {
#if ESP32_WROOM
      private const string SerialPortId = "";
#endif
#if NETDUINO3_WIFI
      private const string SerialPortId = "COM3";
#endif
#if MBN_QUAIL
      private const string SpiBusId = "";
#endif
#if ST_NUCLEO64_F091RC
      private const string SerialPortId = "";
#endif
#if ST_NUCLEO144_F746ZG
      private const string SerialPortId = "";
#endif
#if ST_STM32F429I_DISCOVERY
      private const string SerialPortId = "";
#endif
#if ST_STM32F769I_DISCOVERY
      private const string SerialPortId = "COM6";
#endif

      public static void Main()
      {
         SerialPort serialDevice;

         Debug.WriteLine("devMobile.IoT.Rak811.ShieldSerial starting");

         Debug.Write("Ports:");
         foreach (string port in SerialPort.GetPortNames())
         {
            Debug.Write($" {port}");
         }
         Debug.WriteLine("");

         try
         {
            // set GPIO functions for COM2 (this is UART1 on ESP32)
#if ESP32_WROOM
            Configuration.SetPinFunction(Gpio.IO04, DeviceFunction.COM2_TX);
            Configuration.SetPinFunction(Gpio.IO05, DeviceFunction.COM2_RX);
#endif
            using (serialDevice = new SerialPort(SerialPortId))
            {

               // set parameters
               serialDevice.BaudRate = 9600;
               serialDevice.Parity = Parity.None;
               serialDevice.StopBits = StopBits.One;
               serialDevice.Handshake = Handshake.None;
               serialDevice.DataBits = 8;

               serialDevice.ReadTimeout = 1000;

               serialDevice.NewLine = "\r\n";

               serialDevice.Open();

#if SERIAL_ASYNC_READ
               serialDevice.DataReceived += SerialDevice_DataReceived;
#endif

               // set a watch char to be notified when it's available in the input stream
               serialDevice.WatchChar = '\n';

               while (true)
               {
                  serialDevice.WriteLine("at+version");

#if SERIAL_SYNC_READ
                  string response = serialDevice.ReadLine();

                  Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length} read from {serialDevice.PortName}");
#endif
                  Thread.Sleep(20000);
               }
            }
         }
         catch (Exception ex)
         {
            Debug.WriteLine(ex.Message);
         }
      }

#if SERIAL_ASYNC_READ
      private static void SerialDevice_DataReceived(object sender, SerialDataReceivedEventArgs e)
      {
         switch(e.EventType)
         {
            case SerialData.Chars:
               //Debug.WriteLine("RX SerialData.Chars");
               break;

            case SerialData.WatchChar:
               Debug.WriteLine("RX: SerialData.WatchChar");
               SerialPort serialDevice = (SerialPort)sender;

               string response = serialDevice.ReadExisting();

               Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length} read from {serialDevice.PortName}");
               break;
            default:
               Debug.Assert(false, $"e.EventType {e.EventType} unknown");
               break;
         }
      }
#endif
   }
}
