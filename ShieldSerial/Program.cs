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
//#define SERIAL_SYNC_READ
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
   using System.Threading;
   using Windows.Devices.SerialCommunication;
   using Windows.Storage.Streams;

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
         SerialDevice serialDevice;

         Debug.WriteLine("devMobile.IoT.Rak811.ShieldSerial starting");

         Debug.WriteLine(Windows.Devices.SerialCommunication.SerialDevice.GetDeviceSelector());

         try
         {
            // set GPIO functions for COM2 (this is UART1 on ESP32)
            #if ESP32_WROOM
               Configuration.SetPinFunction(Gpio.IO04, DeviceFunction.COM2_TX);
               Configuration.SetPinFunction(Gpio.IO05, DeviceFunction.COM2_RX);
            #endif
            serialDevice = SerialDevice.FromId(SerialPortId);

            // set parameters
            serialDevice.BaudRate = 9600;
            serialDevice.Parity = SerialParity.None;
            serialDevice.StopBits = SerialStopBitCount.One;
            serialDevice.Handshake = SerialHandshake.None;
            serialDevice.DataBits = 8;

            serialDevice.ReadTimeout = new TimeSpan(0, 0, 30);
            serialDevice.WriteTimeout = new TimeSpan(0, 0, 4);

            DataWriter outputDataWriter = new DataWriter(serialDevice.OutputStream);

            #if SERIAL_SYNC_READ
               DataReader inputDataReader = new DataReader(serialDevice.InputStream);
            #else
               serialDevice.DataReceived += SerialDevice_DataReceived;
            #endif

            // set a watch char to be notified when it's available in the input stream
            serialDevice.WatchChar = '\n';

            while (true)
            {
               uint bytesWritten = outputDataWriter.WriteString("at+version\r\n");
               Debug.WriteLine($"TX: {outputDataWriter.UnstoredBufferLength} bytes to output stream.");

               // calling the 'Store' method on the data writer actually sends the data
               uint txByteCount = outputDataWriter.Store();
               Debug.WriteLine($"TX: {txByteCount} bytes via {serialDevice.PortName}");

#if SERIAL_SYNC_READ
               uint bytesRead = inputDataReader.Load(50);

               Debug.WriteLine($"RXs :{bytesRead} bytes read from {serialDevice.PortName}");

               if (bytesRead > 0)
               {
                  String response = inputDataReader.ReadString(bytesRead);
                  Debug.WriteLine($"RX sync:{response}");
               }
#endif

               Thread.Sleep(20000);
            }
         }
         catch (Exception ex)
         {
            Debug.WriteLine(ex.Message);
         }
      }

      private static void SerialDevice_DataReceived(object sender, SerialDataReceivedEventArgs e)
      {
         switch(e.EventType)
         {
            case SerialData.Chars:
               //Debug.WriteLine("RX SerialData.Chars");
               break;

            case SerialData.WatchChar:
               Debug.WriteLine("RX: SerialData.WatchChar");
               SerialDevice serialDevice = (SerialDevice)sender;

               using (DataReader inputDataReader = new DataReader(serialDevice.InputStream))
               {
                  inputDataReader.InputStreamOptions = InputStreamOptions.Partial;

                  // read all available bytes from the Serial Device input stream
                  uint bytesRead = inputDataReader.Load(serialDevice.BytesToRead);

                  Debug.WriteLine($"RXa: {bytesRead} bytes read from {serialDevice.PortName}");

                  if (bytesRead > 0)
                  {
                     String response = inputDataReader.ReadString(bytesRead);
                     Debug.WriteLine($"RX:{response}");
                  }
               }
               break;
            default:
               Debug.Assert(false, $"e.EventType {e.EventType} unknown");
               break;
         }
      }
   }
}
