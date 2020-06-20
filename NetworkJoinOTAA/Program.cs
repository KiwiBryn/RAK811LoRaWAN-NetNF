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
namespace devMobile.IoT.Rak811.NetworkJoinOTAA
{
   using System;
   using System.Diagnostics;
   using System.Threading;

   using Windows.Devices.SerialCommunication;
   using Windows.Storage.Streams;

   public class Program
   {
      private const string SerialPortId = "COM6";
      private const string devEui = "...";
      private const string appEui = "...";
      private const string appKey = "...";
      private const byte messagePort = 1;
      private const string payload = "48656c6c6f204c6f526157414e"; // Hello LoRaWAN

      public static void Main()
      {
         SerialDevice serialDevice;
         uint bytesWritten;
         uint txByteCount;
         uint bytesRead;

         Debug.WriteLine("devMobile.IoT.Rak811.NetworkJoinOTAA starting");

         Debug.WriteLine(Windows.Devices.SerialCommunication.SerialDevice.GetDeviceSelector());

         try
         {
            serialDevice = SerialDevice.FromId(SerialPortId);

            // set parameters
            serialDevice.BaudRate = 9600;
            serialDevice.Parity = SerialParity.None;
            serialDevice.StopBits = SerialStopBitCount.One;
            serialDevice.Handshake = SerialHandshake.None;
            serialDevice.DataBits = 8;

            serialDevice.ReadTimeout = new TimeSpan(0, 0, 1);
            serialDevice.WriteTimeout = new TimeSpan(0, 0, 4);

            DataWriter outputDataWriter = new DataWriter(serialDevice.OutputStream);
            DataReader inputDataReader = new DataReader(serialDevice.InputStream);

            // set a watch char to be notified when it's available in the input stream
            serialDevice.WatchChar = '\n';

            // clear out the RX buffer
            bytesRead = inputDataReader.Load(128);
            while (bytesRead > 0)
            {
               bytesRead = inputDataReader.Load(128);
               if (bytesRead > 0)
               {
                  string response = inputDataReader.ReadString(bytesRead);
                  Debug.WriteLine($"RX sync:{response}");
               }
            }

            // Set the Working mode to LoRaWAN
            bytesWritten = outputDataWriter.WriteString("at+set_config=lora:work_mode:0\r\n");
            Debug.WriteLine($"TX: work_mode {outputDataWriter.UnstoredBufferLength} bytes to output stream.");
            txByteCount = outputDataWriter.Store();
            Debug.WriteLine($"TX: {txByteCount} bytes via {serialDevice.PortName}");

            // Read the response
            bytesRead = inputDataReader.Load(128);
            if (bytesRead > 0)
            {
               string response = inputDataReader.ReadString(bytesRead);
               Debug.WriteLine($"RX sync:{response}");
            }

            // Set the Region to AS923
            bytesWritten = outputDataWriter.WriteString("at+set_config=lora:region:AS923\r\n");
            Debug.WriteLine($"TX: region {outputDataWriter.UnstoredBufferLength} bytes to output stream.");
            txByteCount = outputDataWriter.Store();
            Debug.WriteLine($"TX: {txByteCount} bytes via {serialDevice.PortName}");

            // Read the response
            bytesRead = inputDataReader.Load(128);
            if (bytesRead > 0)
            {
               String response = inputDataReader.ReadString(bytesRead);
               Debug.WriteLine($"RX sync:{response}");
            }

            // Set the JoinMode
            bytesWritten = outputDataWriter.WriteString($"at+set_config=lora:join_mode:0\r\n");
            Debug.WriteLine($"TX: join_mode {outputDataWriter.UnstoredBufferLength} bytes to output stream.");
            txByteCount = outputDataWriter.Store();
            Debug.WriteLine($"TX: {txByteCount} bytes via {serialDevice.PortName}");

            // Read the response
            bytesRead = inputDataReader.Load(128);
            if (bytesRead > 0)
            {
               String response = inputDataReader.ReadString(bytesRead);
               Debug.WriteLine($"RX sync:{response}");
            }

            // OTAA set the devEUI
            bytesWritten = outputDataWriter.WriteString($"at+set_config=lora:dev_eui:{devEui}\r\n");
            Debug.WriteLine($"TX: dev_eui {outputDataWriter.UnstoredBufferLength} bytes to output stream.");
            txByteCount = outputDataWriter.Store();
            Debug.WriteLine($"TX: {txByteCount} bytes via {serialDevice.PortName}");

            // Read the response
            bytesRead = inputDataReader.Load(128);
            if (bytesRead > 0)
            {
               String response = inputDataReader.ReadString(bytesRead);
               Debug.WriteLine($"RX sync:{response}");
            }

            // Set the appEUI
            bytesWritten = outputDataWriter.WriteString($"at+set_config=lora:app_eui:{appEui}\r\n");
            Debug.WriteLine($"TX: app_eui {outputDataWriter.UnstoredBufferLength} bytes to output stream.");
            txByteCount = outputDataWriter.Store();
            Debug.WriteLine($"TX: {txByteCount} bytes via {serialDevice.PortName}");

            // Read the response
            bytesRead = inputDataReader.Load(128);
            if (bytesRead > 0)
            {
               String response = inputDataReader.ReadString(bytesRead);
               Debug.WriteLine($"RX sync:{response}");
            }

            // Set the appKey
            bytesWritten = outputDataWriter.WriteString($"at+set_config=lora:app_key:{appKey}\r\n");
            Debug.WriteLine($"TX: app_key {outputDataWriter.UnstoredBufferLength} bytes to output stream.");
            txByteCount = outputDataWriter.Store();
            Debug.WriteLine($"TX: {txByteCount} bytes via {serialDevice.PortName}");

            // Read the response
            bytesRead = inputDataReader.Load(128);
            if (bytesRead > 0)
            {
               String response = inputDataReader.ReadString(bytesRead);
               Debug.WriteLine($"RX sync:{response}");
            }


            // Set the Confirm flag
            bytesWritten = outputDataWriter.WriteString($"at+set_config=lora:confirm:0\r\n");
            Debug.WriteLine($"TX: confirm {outputDataWriter.UnstoredBufferLength} bytes to output stream.");
            txByteCount = outputDataWriter.Store();
            Debug.WriteLine($"TX: {txByteCount} bytes via {serialDevice.PortName}");

            // Read the response
            bytesRead = inputDataReader.Load(128);
            if (bytesRead > 0)
            {
               String response = inputDataReader.ReadString(bytesRead);
               Debug.WriteLine($"RX sync:{response}");
            }


            // Join the network
            bytesWritten = outputDataWriter.WriteString($"at+join\r\n");
            Debug.WriteLine($"TX: join {outputDataWriter.UnstoredBufferLength} bytes to output stream.");
            txByteCount = outputDataWriter.Store();
            Debug.WriteLine($"TX: {txByteCount} bytes via {serialDevice.PortName}");

            // Read the response
            bytesRead = inputDataReader.Load(128);
            while (bytesRead == 0)
            {
               bytesRead = inputDataReader.Load(128);

               String response = inputDataReader.ReadString(bytesRead);
               Debug.WriteLine($"RX sync:{response}");
            }

            while (true)
            {
               bytesWritten = outputDataWriter.WriteString($"at+send=lora:{messagePort}:{payload}\r\n");
               Debug.WriteLine($"TX: send {outputDataWriter.UnstoredBufferLength} bytes to output stream.");

               // calling the 'Store' method on the data writer actually sends the data
               txByteCount = outputDataWriter.Store();
               Debug.WriteLine($"TX: {txByteCount} bytes via {serialDevice.PortName}");

               bytesRead = inputDataReader.Load(128);
               if (bytesRead > 0)
               {
                  String response = inputDataReader.ReadString(bytesRead);
                  Debug.WriteLine($"RX sync:{response}");
               }

               Thread.Sleep(20000);
            }
         }
         catch (Exception ex)
         {
            Debug.WriteLine(ex.Message);
         }
      }
   }
}
