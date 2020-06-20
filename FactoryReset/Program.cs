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
//#define HARDWARE_RESET
//#define SOFTWARE_RESTART
//#define DEVICE_STATUS
//#define LORA_STATUS
namespace devMobile.IoT.Rak811.FactoryReset
{
   using System;
   using System.Diagnostics;
   using System.Threading;
   using Windows.Devices.Gpio;
   using Windows.Devices.SerialCommunication;
   using Windows.Storage.Streams;
   
   public class Program
   {
      private const string SerialPortId = "COM6";

      public static void Main()
      {
         SerialDevice serialDevice;

         Debug.WriteLine("devMobile.IoT.Rak811.FactoryReset starting");

         Debug.WriteLine(Windows.Devices.SerialCommunication.SerialDevice.GetDeviceSelector());

         try
         {
#if HARDWARE_RESET
            GpioPin resetPin = GpioController.GetDefault().OpenPin(PinNumber('J', 4));
            resetPin.SetDriveMode(GpioPinDriveMode.Output);
            resetPin.Write(GpioPinValue.Low);
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
#if HARDWARE_RESET
               resetPin.Write(GpioPinValue.High);
               Thread.Sleep(10);
               resetPin.Write(GpioPinValue.Low);
#endif

#if SOFTWARE_RESTART
               uint bytesWritten = outputDataWriter.WriteString("at+set_config=device:restart\r\n");
               Debug.WriteLine($"TX: {outputDataWriter.UnstoredBufferLength} bytes to output stream.");

               // calling the 'Store' method on the data writer actually sends the data
               uint txByteCount = outputDataWriter.Store();
               Debug.WriteLine($"TX: {txByteCount} bytes via {serialDevice.PortName}");
#endif

#if DEVICE_STATUS
               uint bytesWritten = outputDataWriter.WriteString("at+get_config=device:status\r\n");
               Debug.WriteLine($"TX: {outputDataWriter.UnstoredBufferLength} bytes to output stream.");

               // calling the 'Store' method on the data writer actually sends the data
               uint txByteCount = outputDataWriter.Store();
               Debug.WriteLine($"TX: {txByteCount} bytes via {serialDevice.PortName}");
#endif

#if LORA_STATUS
               uint bytesWritten = outputDataWriter.WriteString("at+get_config=lora:status\r\n");
               Debug.WriteLine($"TX: {outputDataWriter.UnstoredBufferLength} bytes to output stream.");

               // calling the 'Store' method on the data writer actually sends the data
               uint txByteCount = outputDataWriter.Store();
               Debug.WriteLine($"TX: {txByteCount} bytes via {serialDevice.PortName}");
#endif

#if SERIAL_SYNC_READ
               // June 2020 appears to be limited to 256 chars
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
         switch (e.EventType)
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
      static int PinNumber(char port, byte pin)
      {
         if (port < 'A' || port > 'J')
            throw new ArgumentException();

         return ((port - 'A') * 16) + pin;
      }
   }
}
