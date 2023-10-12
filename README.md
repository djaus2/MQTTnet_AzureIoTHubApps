# MQTTnet for Azure IoT Hub Apps

## About
Send Telemetry to an Azure IoT Hub using MQTTnet API. Also send CD Messages as well.

- .Net Core 3.1 Console **_Works_**
-   - Autogenerates SasToken from Secrets.cs (Device Primary Key) and uses that
- GenerateSas **_Works_**
  - Generate SasToken, as per Console app, and copies to clipbaord for Medow app.
- Meadow app. **_Works_**
  - Now using Meadow.MQTTnet Nuget package rather than MQTTnet package..
  - _Does not use MQTTnet.Extensions.ManagedClient (as does Console app) as not supported._
  - _SasTokens generated here don't connect. So paste token from GenerateSas app in Secrets.cs_

## Links
- https://github.com/dotnet/MQTTnet _(MQTTnet repository)_
- [Communicate with an IoT hub using the MQTT protocol](https://learn.microsoft.com/en-us/azure/iot/iot-mqtt-connect-to-iot-hub)
- https://dev.to/eduardojuliao/basic-mqtt-with-c-1f88 _(Console Code was derived from this)_
- Meadow Apps Start here: [djaus2/Meadow.ProjectLab.Extensions](https://github.com/djaus2/Meadow.ProjectLab.Extensions)

## Update
Now includes method to generate Sas Token from Device Primary Key. Fails on Meadow so use Powershell on AzCli to get fixed value.

## Sample Secrets.cs
```cs
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace MeadowApplication3
{

    public static class Secrets
    {
        /* Run GenerateSas first which provides a SasToken on the ClipBoard, so paste here then.      
         Alternative: Powershell Command to get SAS Token
         az iot hub generate-sas-token --hub-name HUBNAME --device-id DEVICEID --resource-group AZURERESOURCEGROUP --login IOTHUBCONNECTIONSTRING
         Also -du optional parameter for duration: Valid token duration in seconds.  Default: 3600, 1 hr
       */
        public static bool useThisSasToken = true;
        public static string SasToken = "";
        public static bool UseMQTTnetAPI = true; 
        public static int TelemetryPeriod = 3333;
        public static string WIFI_NAME = "";
        public static string WIFI_PASSWORD = "";

        public static string IOT_CONFIG_IOTHUB_FQDN = "HUBNAME.azure-devices.net";

        public static string DeviceId = "";
        public static string HubName = "";
        public static string DevicePrimaryKey = ""; 
        public static int MqttPort = 8883;

        //INFO https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-c2d
        public static string subTopic = "devices/+/messages/devicebound/#";

        //INFO https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-d2c
        public static string pubTopic = $"\"devices/{DeviceId}/messages/events/\"";

        public static string methodTopic =$"\"$iothub/methods/POST/#\"";
  }
}

```

