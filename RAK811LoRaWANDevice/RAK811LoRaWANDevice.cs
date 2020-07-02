﻿//---------------------------------------------------------------------------------
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
//#define DIAGNOSTICS
namespace devMobile.IoT.LoRaWan
{
   using System;
   using System.Diagnostics;
   using System.Text;
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
      ATResponseTimeout,
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

      private const int CommandTimeoutDefault = 3000;

      private SerialDevice serialDevice = null;
      private TimeSpan ReadTimeoutDefault = new TimeSpan(0, 0, 1);
      private TimeSpan WriteTimeoutDefault = new TimeSpan(0, 0, 2);
      private DataReader inputDataReader = null;
      private DataWriter outputDataWriter = null;

      private string atCommandExpectedResponse;
      private string response;
      private Result result;
      private readonly AutoResetEvent atExpectedEvent;

      public delegate void MessageConfirmationHandler(int rssi, int snr);
      public MessageConfirmationHandler OnMessageConfirmation;
      public delegate void ReceiveMessageHandler(int port, int rssi, int snr, string payload);
      public ReceiveMessageHandler OnReceiveMessage;

      public Rak811LoRaWanDevice()
      {
         this.atExpectedEvent = new AutoResetEvent(false);
      }

      public Result Initialise(string serialPortId, uint baudRate, SerialParity serialParity, ushort dataBits, SerialStopBitCount stopBitCount,
         TimeSpan readTimeout = default, TimeSpan writeTimeout = default)
      {
         Result result;
         Debug.Assert(serialPortId != null);
         Debug.Assert(dataBits > 0);

         serialDevice = SerialDevice.FromId(serialPortId);

         // set parameters
         serialDevice.BaudRate = baudRate;
         serialDevice.Parity = serialParity;
         serialDevice.DataBits = dataBits;
         serialDevice.StopBits = stopBitCount;
         serialDevice.Handshake = SerialHandshake.None;
         serialDevice.WatchChar = '\n';

         response = string.Empty;
         atCommandExpectedResponse = string.Empty;

         serialDevice.DataReceived += SerialDevice_DataReceived;

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

         uint bytesRead;

#if DIAGNOSTICS
         Debug.WriteLine(" Buffer empty start");
#endif
         do
         {
            bytesRead = inputDataReader.Load(128);
            if (bytesRead > 0)
            {
               response += inputDataReader.ReadString(bytesRead);
               Debug.Write(".");
            }
         }
         while (bytesRead > 0);
#if DIAGNOSTICS
         Debug.WriteLine("");
         Debug.WriteLine(" Buffer empty Done");
#endif

         // Set the Working mode to LoRaWAN
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:work_mode LoRaWAN");
#endif
         result = SendCommand("Initialization OK", "at+set_config=lora:work_mode:0\r\n", CommandTimeoutDefault);
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
            // Currently ClassB unsupported
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
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:class {loRaClass}");
#endif
         Result result = SendCommand("OK", command, CommandTimeoutDefault);
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
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:confirm {loRaConfirmType}");
#endif
         Result result = SendCommand("OK", command, CommandTimeoutDefault);
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

#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:region {regionID}");
#endif
         Result result = SendCommand("OK", $"at+set_config=lora:region:{regionID}\r\n", CommandTimeoutDefault);
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result Sleep()
      {
         // Put the RAK module to sleep
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} device:sleep:1");
#endif
         Result result = SendCommand("OK Sleep", $"at+set_config=device:sleep:1\r\n", CommandTimeoutDefault);
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result Wakeup()
      {
         // Wakeup the RAK Module
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} device:sleep:0");
#endif
         Result result = SendCommand("OK Wake Up", $"at+set_config=device:sleep:0\r\n", CommandTimeoutDefault);
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result AdrOff()
      {
         // Adaptive Data Rate off
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:adr:0");
#endif
         Result result = SendCommand("OK", $"at+set_config=lora:adr:0\r\n", CommandTimeoutDefault);
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result AdrOn()
      {
         // Adaptive Data Rate on
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:adr:1");
#endif
         Result result = SendCommand("OK", $"at+set_config=lora:adr:1\r\n", CommandTimeoutDefault);
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
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:join_mode ABP");
#endif
         result = SendCommand("OK", $"at+set_config=lora:join_mode:1\r\n", CommandTimeoutDefault);
         if (result != Result.Success)
         {
            return result;
         }

         // set the devAddr
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:devAddr {devAddr}");
#endif
         result = SendCommand("OK", $"at+set_config=lora:dev_addr:{devAddr}\r\n", CommandTimeoutDefault);
         if (result != Result.Success)
         {
            return result;
         }

         // Set the nwsKey
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:nwks_Key {nwksKey}");
#endif
         result = SendCommand("OK", $"at+set_config=lora:nwks_key:{nwksKey}\r\n", CommandTimeoutDefault);
         if (result != Result.Success)
         {
            return result;
         }

         // Set the appsKey
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:apps_key {appsKey}");
#endif
         result = SendCommand("OK", $"at+set_config=lora:apps_key:{appsKey}\r\n", CommandTimeoutDefault);
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
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:join_mode OTAA");
#endif
         result = SendCommand("OK", $"at+set_config=lora:join_mode:0\r\n", CommandTimeoutDefault);
         if (result != Result.Success)
         {
            return result;
         }

         // set the devEUI
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:dev_eui {devEui}");
#endif
         result = SendCommand("OK", $"at+set_config=lora:dev_eui:{devEui}\r\n", CommandTimeoutDefault);
         if (result != Result.Success)
         {
            return result;
         }

         // Set the appEUI
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:app_eui {appEui}");
#endif
         result = SendCommand("OK", $"at+set_config=lora:app_eui:{appEui}\r\n", CommandTimeoutDefault);
         if (result != Result.Success)
         {
            return result;
         }

         // Set the appKey
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:app_key {appKey}");
#endif
         result = SendCommand("OK", $"at+set_config=lora:app_key:{appKey}\r\n", CommandTimeoutDefault);
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
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} join");
#endif
         result = SendCommand("OK Join Success", $"at+join\r\n", (int)timeout.TotalMilliseconds);
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result Send(ushort port, string payload, TimeSpan timeout)
      {
         Result result;

         // Send message the network
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} Send port:{port} payload {payload} timeout {timeout:hh:mm:ss}");
#endif
         result = SendCommand("OK", $"at+send=lora:{port}:{payload}\r\n", (int)timeout.TotalMilliseconds);
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      public Result Send(ushort port, byte[] payloadBytes, TimeSpan timeout)
      {
         string payloadBcd = Rak811LoRaWanDevice.BytesToBcd(payloadBytes);
         Result result;

         // Send message the network
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} Send port:{port} payload {payload} timeout {timeout:hh:mm:ss}");
#endif
         result = SendCommand("OK", $"at+send=lora:{port}:{payloadBcd}\r\n", (int)timeout.TotalMilliseconds);
         if (result != Result.Success)
         {
            return result;
         }

         return Result.Success;
      }

      private Result SendCommand(string expectedResponse, string command, int timeout)
      {
         this.atCommandExpectedResponse = expectedResponse;

         outputDataWriter.WriteString(command);
         outputDataWriter.Store();

         this.atExpectedEvent.Reset();

         if (!this.atExpectedEvent.WaitOne(timeout, false))
            return Result.ATResponseTimeout;

         this.atCommandExpectedResponse = string.Empty;

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

      private void SerialDevice_DataReceived(object sender, SerialDataReceivedEventArgs e)
      {
         // we only care if got EoL character
         if (e.EventType != SerialData.WatchChar)
         {
            return;
         }

         SerialDevice serialDevice = (SerialDevice)sender;

         using (DataReader inputDataReader = new DataReader(serialDevice.InputStream))
         {
            inputDataReader.InputStreamOptions = InputStreamOptions.Partial;

            // read all available bytes from the Serial Device input stream
            var bytesRead = inputDataReader.Load(serialDevice.BytesToRead);
            if (bytesRead == 0)
            {
               return;
            }

            response += inputDataReader.ReadString(bytesRead);

            int eol;
            do
            {
               // extract a line
               eol = response.IndexOf("\r\n");

               if (eol != -1)
               {
                  string line = response.Substring(0, eol + "\r\n".Length);
                  response = response.Substring(line.Length);
                  line = line.Trim();

#if DIAGNOSTICS
                  Debug.WriteLine($" Line :{line} ATCommand:{atCommandExpectedResponse} Response:{response}");
#endif
                  int errorIndex = line.IndexOf("ERROR:");
                  if (errorIndex != -1)
                  {
                     string errorNumber = line.Substring(errorIndex + "ERROR:".Length);

                     result = ModemErrorParser(errorNumber.Trim());
                     atExpectedEvent.Set();
                  }

                  if (atCommandExpectedResponse != string.Empty)
                  {
                     int successIndex = line.IndexOf(atCommandExpectedResponse);
                     if (successIndex != -1)
                     {
                        result = Result.Success;
                        atExpectedEvent.Set();
                     }
                  }

                  int receivedMessageIndex = line.IndexOf("at+recv=");
                  if (receivedMessageIndex != -1)
                  {
                     string[] fields = line.Split("=,:".ToCharArray());

                     int port = int.Parse(fields[1]);
                     int rssi = int.Parse(fields[2]);
                     int snr = int.Parse(fields[3]);
                     int length = int.Parse(fields[4]);

                     if (this.OnMessageConfirmation != null)
                     {
                        OnMessageConfirmation(rssi, snr);
                     }
                     if (length > 0)
                     {
                        string payload = fields[5];

                        if (this.OnReceiveMessage != null)
                        {
                           OnReceiveMessage(port, rssi, snr, payload);
                        }
                     }
                  }
               }
            }
            while (eol != -1);
         }
      }

      // Utility functions for clients for processing messages payloads to be send, ands messages payloads received.

      public static string BytesToBcd(byte[] payloadBytes)
      {
         Debug.Assert(payloadBytes != null);
         Debug.Assert(payloadBytes.Length > 0);

         StringBuilder payloadBcd = new StringBuilder(BitConverter.ToString(payloadBytes));

         payloadBcd = payloadBcd.Replace("-", "");

         return payloadBcd.ToString();
      }

      public static byte[] BcdToByes(string payloadBcd)
      {
         Debug.Assert(payloadBcd != null);
         Debug.Assert(payloadBcd != String.Empty);
         Debug.Assert(payloadBcd.Length % 2 == 0);
         Byte[] payloadBytes = new byte[payloadBcd.Length / 2];

         char[] chars = payloadBcd.ToCharArray();

         for (int index = 0; index < payloadBytes.Length; index++)
         {
            byte byteHigh = Convert.ToByte(chars[index * 2].ToString(), 16);
            byte byteLow = Convert.ToByte(chars[(index * 2) + 1].ToString(), 16);

            payloadBytes[index] += (byte)(byteHigh * 16);
            payloadBytes[index] += byteLow;
         }

         return payloadBytes;
      }

      public void Dispose()
      {
      }
   }
}
