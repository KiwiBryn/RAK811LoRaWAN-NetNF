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
namespace devMobile.IoT.Rak811.NetworkJoinABP
{
   using System;
   using System.Diagnostics;
   using System.IO.Ports;
   using System.Threading;

   public class Program
   {
      private const string SerialPortId = "COM6";
      private const string DevAddress = "...";
      private const string NwksKey = "...";
      private const string AppsKey = "...";
      private const byte MessagePort = 1;
      private const string Payload = "48656c6c6f204c6f526157414e"; // Hello LoRaWAN

      public static void Main()
      {
         string response;

         Debug.WriteLine("devMobile.IoT.Rak811.NetworkJoinABP starting");

         Debug.Write("Ports:");
         foreach (string port in SerialPort.GetPortNames())
         {
            Debug.Write($" {port}");
         }
         Debug.WriteLine("");

         try
         {
            using (SerialPort serialDevice = new SerialPort(SerialPortId))
            {
               // set parameters
               serialDevice.BaudRate = 9600;
               serialDevice.Parity = Parity.None;
               serialDevice.StopBits = StopBits.One;
               serialDevice.Handshake = Handshake.None;
               serialDevice.DataBits = 8;

               //serialDevice.ReadTimeout = 5000;
               serialDevice.ReadTimeout = 10000;

               serialDevice.NewLine = "\r\n";

               serialDevice.Open();

               // clear out the RX buffer
               serialDevice.ReadExisting();
               response = serialDevice.ReadExisting();
               Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");
               Thread.Sleep(500);

               // Set the Working mode to LoRaWAN
               serialDevice.WriteLine("at+set_config=lora:work_mode:0");
               Thread.Sleep(5000);
               response = serialDevice.ReadExisting();
               response = response.Trim('\0');
               Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

               // Set the Region to AS923
               serialDevice.WriteLine("at+set_config=lora:region:AS923");
               response = serialDevice.ReadLine();
               Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

               // Set the JoinMode to ABP
               serialDevice.WriteLine("at+set_config=lora:join_mode:1");
               response = serialDevice.ReadLine();
               Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

               // Set the device address
               serialDevice.WriteLine($"at+set_config=lora:dev_addr:{DevAddress}");
               response = serialDevice.ReadLine();
               Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

               // Set the network session key
               serialDevice.WriteLine($"at+set_config=lora:nwks_key:{NwksKey}");
               response = serialDevice.ReadLine();
               Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

               // Set the application session key
               serialDevice.WriteLine($"at+set_config=lora:apps_key:{AppsKey}");
               response = serialDevice.ReadLine();
               Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

               // Set the Confirm flag
               serialDevice.WriteLine("at+set_config=lora:confirm:0");
               response = serialDevice.ReadLine();
               Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

               // Join the network
               serialDevice.WriteLine("at+join");
               Thread.Sleep(10000);
               response = serialDevice.ReadLine();
               Debug.WriteLine($"Response :{response.Trim()} bytes:{response.Length}");

               while (true)
               {
                  // Send the BCD messages
                  serialDevice.WriteLine($"at+send=lora:{MessagePort}:{Payload}");
                  Thread.Sleep(1000);

                  // The OK
                  //response = serialDevice.ReadLine();
                  response = serialDevice.ReadExisting();
                  Debug.WriteLine($"Response :{response.Trim()} bytes:{response.Length}");

                  Thread.Sleep(20000);
               }
            }
         }
         catch (Exception ex)
         {
            Debug.WriteLine(ex.Message);
         }
      }
   }
}
