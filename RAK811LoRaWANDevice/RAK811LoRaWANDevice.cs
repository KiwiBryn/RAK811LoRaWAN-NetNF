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
namespace devMobile.IoT.LoRaWan
{
   using System;
   using System.Diagnostics;
   using System.Threading;

   using Windows.Devices.SerialCommunication;
   using Windows.Storage.Streams;

   public enum LoRaClass
   {
      Undefined = 0,
      A,
      B,
      C
   }

   public enum LoRaConfirmType
   {
      Undefined = 0,
      Unconfirmed,
      Confirmed,
      Multicast,
      Proprietory
   }

   public enum Result
   {
      Undefined = 0,
      Success,
      ResponseInvalid,
      ResponseTimeout,
      ATCommandUnsuported,
      ATCommandInvalidParameter,

      LoRaBusy,
      LoRaServiceIsUnknown,
      LoRaParameterInvalid,
      LoRaFrequencyInvalid,
      LoRaDataRateInvalid,
      LoRaFrequencyAndDataRateInvalid,
      LoRaDeviceNotJoinedNetwork,
      LoRaPacketToLong,
      LoRaServiceIsClosedByServer,
      LoRaRegionUnsupported,
      LoRaDutyCycleRestricted,
      LoRaNoValidChannelFound,
      LoRaNoFreeChannelFound,
      StatusIsError,
      LoRaTransmitTimeout,
      LoRaRX1Timeout,
      LoRaRX2Timeout,
      LoRaRX1ReceiveError,
      LoRaRX2ReceiveError,
      LoRaJoinFailed,
      LoRaDownlinkRepeated,
      LoRaInvalidPayloadSizeForDataRate,
      LoRaTooManyDownlinkFramesLost,
      LoRaAddressFail,
      LoRaMicVerifyError,
   }

   public class Rak811LoRaWanDevice : IDisposable
   {
      public const ushort RegionIDLength = 5;
      public const ushort DevEuiLength = 16;
      public const ushort AppEuiLength = 16;
      public const ushort AppKeyLength = 32;
      public const ushort DevAddrLength = 8;
      public const ushort NwsKeyLength = 32;
      public const ushort AppsKeyLength = 32;

      private SerialDevice serialDevice = null;
      private TimeSpan ReadTimeoutDefault = new TimeSpan(0, 0, 1);
      private TimeSpan WriteTimeoutDefault = new TimeSpan(0, 0, 2);
      private DataReader inputDataReader = null;
      private DataWriter outputDataWriter = null;

      public Result Initialise(string serialPortId, SerialParity serialParity, ushort dataBits, SerialStopBitCount stopBitCount,
         TimeSpan readTimeout = default, TimeSpan writeTimeout = default)
      {
         Result result;
         Debug.Assert(serialPortId != null);
         Debug.Assert(dataBits > 0);

         serialDevice = SerialDevice.FromId(serialPortId);

         // set parameters
         serialDevice.BaudRate = 9600;
         serialDevice.Parity = serialParity;
         serialDevice.DataBits = dataBits;
         serialDevice.StopBits = stopBitCount;
         serialDevice.Handshake = SerialHandshake.None;
         serialDevice.WatchChar = '\n';

         if (readTimeout == default)
         {
            serialDevice.ReadTimeout = ReadTimeoutDefault;
         }

         if (writeTimeout == default)
         {
            serialDevice.WriteTimeout = WriteTimeoutDefault;
         }

         outputDataWriter = new DataWriter(serialDevice.OutputStream);
         inputDataReader = new DataReader(serialDevice.InputStream);

         // Set the Working mode to LoRaWAN
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:work_mode");
         result = SendCommand("Initialization OK", "at+set_config=lora:work_mode:0\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result Class(LoRaClass loRaClass)
      {
         string command;
         Debug.Assert(loRaClass != LoRaClass.Undefined);

         switch (loRaClass)
         {
            case LoRaClass.A:
               command = "at+set_config=lora:class:0\r\n";
               break;
            //case LoRaClass.B;
            //   command = "at+set_config=lora:class:1\r\n";
            //   break;
            case LoRaClass.C:
               command = "at+set_config=lora:class:2\r\n";
               break;
            default:
               throw new ArgumentException($"LoRa class value {loRaClass} invalid", "loRaClass");
         }

         // Set the class
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:class");
         Result result = SendCommand("OK", command);
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result Confirm(LoRaConfirmType loRaConfirmType)
      {
         string command;
         Debug.Assert(loRaConfirmType != LoRaConfirmType.Undefined);

         switch (loRaConfirmType)
         {
            case LoRaConfirmType.Unconfirmed:
               command = "at+set_config=lora:confirm:0\r\n";
               break;
            case LoRaConfirmType.Confirmed:
               command = "at+set_config=lora:confirm:1\r\n";
               break;
            case LoRaConfirmType.Multicast:
               command = "at+set_config=lora:confirm:2\r\n";
               break;
            case LoRaConfirmType.Proprietory:
               command = "at+set_config=lora:confirm:3\r\n";
               break;
            default:
               throw new ArgumentException($"LoRa confirm type value {loRaConfirmType} invalid", "loRaConfirmType");
         }

         // Set the confirmation type
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:confirm");
         Result result = SendCommand("OK", command);
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result Region(string regionID)
      {
         Debug.Assert(regionID != null);

         if (regionID.Length != RegionIDLength)
         {
            throw new ArgumentException($"RegionID length {regionID.Length} invalid", "regionID");
         }

         // Set the Region to AS923
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:region");
         Result result = SendCommand("OK", $"at+set_config=lora:region:{regionID}\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result Sleep()
      {
         // Put the module to sleep
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} device:sleep:1");
         Result result = SendCommand("OK Sleep", $"at+set_config=device:sleep:1\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result Wakeup()
      {
         // Put the module to sleep
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} device:sleep:0");
         Result result = SendCommand("OK Wake Up", $"at+set_config=device:sleep:0\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result AdrOff()
      {
         // Put the module to sleep
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:adr:0");
         Result result = SendCommand("OK", $"at+set_config=lora:adr:0\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result AdrOn()
      {
         // Put the module to sleep
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:adr:1");
         Result result = SendCommand("OK", $"at+set_config=lora:adr:1\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result AbpInitialise(string devAddr, string nwksKey, string appsKey)
      {
         Result result;

         if (devAddr.Length != DevAddrLength)
         {
            throw new ArgumentException($"devAddr invalid length must be {DevAddrLength} characters", "devAddr");
         }
         if (nwksKey.Length != NwsKeyLength)
         {
            throw new ArgumentException($"nwsKey invalid length must be {NwsKeyLength} characters", "nwsKey");
         }
         if (appsKey.Length != AppsKeyLength)
         {
            throw new ArgumentException($"appsKey invalid length must be {AppsKeyLength} characters", "appsKey");
         }

         // Set the JoinMode to ABP
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:join_mode");
         result = SendCommand("OK", $"at+set_config=lora:join_mode:1\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         // set the devAddr
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:devAddr");
         result = SendCommand("OK", $"at+set_config=lora:dev_addr:{devAddr}\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         // Set the nwsKey
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:nwks_Key");
         result = SendCommand("OK", $"at+set_config=lora:nwks_key:{nwksKey}\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         // Set the appsKey
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:apps_key");
         result = SendCommand("OK", $"at+set_config=lora:apps_key:{appsKey}\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result OtaaInitialise(string devEui, string appEui, string appKey)
      {
         Result result;

         if (devEui.Length != DevEuiLength)
         {
            throw new ArgumentException($"devEui invalid length must be {DevEuiLength} characters", "devEui");
         }
         if (appEui.Length != AppEuiLength)
         {
            throw new ArgumentException($"appEui invalid length must be {AppEuiLength} characters", "appEui");
         }
         if (appKey.Length != AppKeyLength)
         {
            throw new ArgumentException($"appKey invalid length must be {AppKeyLength} characters", "appKey");
         }

         // Set the JoinMode to OTAA
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:join_mode");
         result = SendCommand("OK", $"at+set_config=lora:join_mode:0\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         // set the devEUI
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:dev_eui");
         result = SendCommand("OK", $"at+set_config=lora:dev_eui:{devEui}\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         // Set the appEUI
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:app_eui");
         result = SendCommand("OK", $"at+set_config=lora:app_eui:{appEui}\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         // Set the appKey
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} lora:app_key");
         result = SendCommand("OK", $"at+set_config=lora:app_key:{appKey}\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result Join(TimeSpan timeout)
      {
         Result result;

         // Join the network
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} join");
         result = SendCommand("OK Join Success", $"at+join\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result Send(ushort port, string payload)
      {
         Result result;

         // Send message the network
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} send");
         result = SendCommand("OK", $"at+send=lora:{port}:{payload}\r\n");
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      private Result SendCommand(string success, string command)
      {
         uint bytesWritten;
         uint txByteCount;
         uint bytesRead;
         string response = string.Empty;
         Result result = Result.Undefined;

         bytesWritten = outputDataWriter.WriteString(command);
         txByteCount = outputDataWriter.Store();
         Debug.WriteLine($"TX: {bytesWritten} bytes send {outputDataWriter.UnstoredBufferLength} bytes {txByteCount} via {serialDevice.PortName}");

         while (result == Result.Undefined)
         {
            bytesRead = inputDataReader.Load(128);
            if (bytesRead > 0)
            {
               response += inputDataReader.ReadString(bytesRead);
            }
            Debug.WriteLine($"RX {DateTime.UtcNow:hh:mm:ss}:{response}");

            int errorIndex = response.IndexOf("ERROR:");
            if (errorIndex != -1)
            {
               string errorNumber = response.Substring(errorIndex + "ERROR:".Length);

               result = ModemErrorParser(errorNumber.Trim());
            }

            int successIndex = response.IndexOf(success);
            if (successIndex != -1)
            {
                  result = Result.Success;
            }
            Thread.Sleep(500);
         }

         return result;
      }

      private Result ModemErrorParser(string errorText)
      {
         Result result;
         ushort errorNumber;

         try
         {
            errorNumber = ushort.Parse(errorText);
         }
         catch (Exception)
         {
            return Result.ResponseInvalid;
         }

         switch (errorNumber)
         {
            case 1:
               result = Result.ATCommandUnsuported;
               break;
            case 2:
               result = Result.ATCommandInvalidParameter;
               break;
            case 3: //There is an error when reading or writing flash.
            case 4: //There is an error when reading or writing through IIC.
            case 5: //There is an error when sending through UART
            case 41: //The BLE works in an invalid state, so that it can’t be operated.
               result = Result.ResponseInvalid;
               break;
            case 80:
               result = Result.LoRaBusy;
               break;
            case 81:
               result = Result.LoRaServiceIsUnknown;
               break;
            case 82:
               result = Result.LoRaParameterInvalid;
               break;
            case 83:
               result = Result.LoRaFrequencyInvalid;
               break;
            case 84:
               result = Result.LoRaDataRateInvalid;
               break;
            case 85:
               result = Result.LoRaInvalidPayloadSizeForDataRate;
               break;
            case 86:
               result = Result.LoRaDeviceNotJoinedNetwork;
               break;
            case 87:
               result = Result.LoRaPacketToLong;
               break;
            case 88:
               result = Result.LoRaServiceIsClosedByServer;
               break;
            case 89:
               result = Result.LoRaRegionUnsupported;
               break;
            case 90:
               result = Result.LoRaDutyCycleRestricted;
               break;
            case 91:
               result = Result.LoRaNoValidChannelFound;
               break;
            case 92:
               result = Result.LoRaNoFreeChannelFound;
               break;
            case 93:
               result = Result.StatusIsError;
               break;
            case 94:
               result = Result.LoRaTransmitTimeout;
               break;
            case 95:
               result = Result.LoRaRX1Timeout;
               break;
            case 96:
               result = Result.LoRaRX2Timeout;
               break;
            case 97:
               result = Result.LoRaRX1ReceiveError;
               break;
            case 98:
               result = Result.LoRaRX2ReceiveError;
               break;
            case 99:
               result = Result.LoRaJoinFailed;
               break;
            case 101:
               result = Result.LoRaInvalidPayloadSizeForDataRate;
               break;
            case 102:
               result = Result.LoRaTooManyDownlinkFramesLost;
               break;
            case 103:
               result = Result.LoRaAddressFail;
               break;
            case 104:
               result = Result.LoRaMicVerifyError;
               break;
            default:
               result = Result.ResponseInvalid;
               break;
         }

         return result;
      }


      public void Dispose()
      {
      }
   }
}
