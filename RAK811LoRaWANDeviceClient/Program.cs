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
#define ST_STM32F769I_DISCOVERY      // nanoff --target ST_STM32F769I_DISCOVERY --update 
#define OTAA
//#define ABP
namespace devMobile.IoT.Rak811LoRaWanDeviceClient
{
   using System;
   using System.Threading;
   using System.Diagnostics;
   using Windows.Devices.SerialCommunication;

   using devMobile.IoT.LoRaWan;

   public class Program
   {
#if ST_STM32F769I_DISCOVERY
      private const string SerialPortId = "COM6";
#endif
#if OTAA
      private const string DevEui = "...";
      private const string AppEui = "...";
      private const string AppKey = "...";
#endif
#if ABP
      private const string devAddress = "...";
      private const string nwksKey = "...";
      private const string appsKey = "...";
#endif
      private const byte MessagePort = 1;
      private const string Payload = "48656c6c6f204c6f526157414e"; // Hello LoRaWAN

      public static void Main()
      {
         Result result;

         Debug.WriteLine(" devMobile.IoT.Rak811LoRaWanDeviceClient starting");

         Debug.WriteLine(Windows.Devices.SerialCommunication.SerialDevice.GetDeviceSelector());

         try
         {
            using ( Rak811LoRaWanDevice device = new Rak811LoRaWanDevice())
            {
               result = device.Initialise(SerialPortId, SerialParity.None, 8, SerialStopBitCount.One);
               if (result != Result.Success)
               {
                  Debug.WriteLine($"Initialise failed {result}");
                  return;
               }

               result = device.Region("AS923");
               if (result != Result.Success)
               {
                  Debug.WriteLine($"Region failed {result}");
                  return;
               }

               result = device.AdrOn();
               if (result != Result.Success)
               {
                  Debug.WriteLine($"ADR on failed {result}");
                  return;
               }

#if OTAA
               result = device.OtaaInitialise(DevEui, AppEui, AppKey);
               if (result != Result.Success)
               {
                  Debug.WriteLine($"OTAA Initialise failed {result}");
                  return;
               }
#endif

#if ABP
               result = device.AbpInitialise(devAddress, nwksKey, appsKey);
               if (result != Result.Success)
               {
                  Debug.WriteLine($"ABP Initialise failed {result}");
                  return;
               }
#endif

               result = device.Join(new TimeSpan(0,0,10));
               if (result != Result.Success)
               {
                  Debug.WriteLine($"Join failed {result}");
                  return;
               }

               while (true)
               {
                  result = device.Send(MessagePort, Payload);
                  if (result != Result.Success)
                  {
                     Debug.WriteLine($"Send failed {result}");
                  }

                  result = device.Sleep();
                  if (result != Result.Success)
                  {
                     Debug.WriteLine($"Sleep failed {result}");
                     return;
                  }

                  Thread.Sleep(20000);

                  result = device.Wakeup();
                  if (result != Result.Success)
                  {
                     Debug.WriteLine($"Wakeup failed {result}");
                     return;
                  }
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
