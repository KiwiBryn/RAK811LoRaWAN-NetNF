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
// nanoff --target ST_STM32F769I_DISCOVERY --update
//#define SERIAL_SYNC_READ
//#define SERIAL_ASYNC_READ
//#define HARDWARE_RESET
//#define SOFTWARE_RESTART
//#define DEVICE_STATUS
//#define LORA_STATUS
namespace devMobile.IoT.Rak811.FactoryReset
{
   using System;
   using System.Diagnostics;
   using System.IO.Ports;
   using System.Threading;
#if HARDWARE_RESET
   using System.Device.Gpio;
#endif
  
   public class Program
   {
      private const string SerialPortId = "COM6";

      public static void Main()
      {
         Debug.WriteLine("devMobile.IoT.Rak811.FactoryReset starting");
#if HARDWARE_RESET
         GpioController gpioController = new GpioController();
#endif
      
         Debug.Write("Ports:");
         foreach (string port in SerialPort.GetPortNames())
         {
            Debug.Write($" {port}");
         }
         Debug.WriteLine("");

         try
         {
#if HARDWARE_RESET
            GpioPin resetPin = gpioController.OpenPin(PinNumber('J', 4));
            gpioController.SetPinMode(resetPin.PinNumber, PinMode.Output);
            resetPin.Write(PinValue.Low);
#endif

            using (SerialPort serialDevice = new SerialPort(SerialPortId))
            {
               // set parameters
               serialDevice.BaudRate = 9600;
               serialDevice.Parity = Parity.None;
               serialDevice.StopBits = StopBits.One;
               serialDevice.Handshake = Handshake.None;
               serialDevice.DataBits = 8;

               serialDevice.ReadTimeout = 5000;

               serialDevice.NewLine = "\r\n";

               serialDevice.Open();

#if SERIAL_ASYNC_READ
               serialDevice.DataReceived += SerialDevice_DataReceived;

               // set a watch char to be notified when it's available in the input stream
               serialDevice.WatchChar = '\n';
#endif

               while (true)
               {
#if HARDWARE_RESET
                  resetPin.Write(PinValue.High);
                  Thread.Sleep(10);
                  resetPin.Write(PinValue.Low);
#endif

#if SOFTWARE_RESTART
                  serialDevice.WriteLine("at+set_config=device:restart");
#endif

#if DEVICE_STATUS
                  serialDevice.WriteLine("at+get_config=device:status");
#endif

#if LORA_STATUS
                  serialDevice.WriteLine("at+get_config=lora:status");
#endif

#if SERIAL_SYNC_READ
                  Thread.Sleep(500);

                  string response = serialDevice.ReadExisting();

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

#if HARDWARE_RESET
      static int PinNumber(char port, byte pin)
      {
         if (port < 'A' || port > 'J')
            throw new ArgumentException();

         return ((port - 'A') * 16) + pin;
      }
#endif
   }
}
