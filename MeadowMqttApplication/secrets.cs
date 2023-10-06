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
        public static bool UseMQTTnetAPI = true; //false cashes in console app
        public static int TelemetryPeriod = 3333;
        public static string WIFI_NAME = "APQLZM";
        public static string WIFI_PASSWORD = "silly1371";

        public static string IOT_CONFIG_IOTHUB_FQDN = "ozz2hub.azure-devices.net";

        public static string IoTHubConnectionString = "HostName=ozz2hub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=hch4P+v0uWj+cjkAEcU36+wAF2eloRQEPAIoTHigvMM=";

        public static string DEVICE_CONNECTION_STRING = "HostName=ozz2hub.azure-devices.net;DeviceId=ozz2dev;SharedAccessKey=+YIjU0fxVC7GM33PaukDyTvPT0dvCk7e9RKTmLInR0E=";

        public static string deviceId = "ozz2dev";
        public static string hubName = "ozz2hub";
        public static string sasToken = "SharedAccessSignature sr=ozz2hub.azure-devices.net%2Fdevices%2Fozz2dev&sig=VKqYYY%2BaikDFZTnrTcfBKIwmrvByAKbjukj2G6WhGY8%3D&se=1696573101";
        public static int MqttPort = 8883;


        //UserName = "myIoTHub.azure-devices.net/device1/api-version=2018-06-30",
        //Password = "SharedAccessSignature sr=myIoTHub.azure-devices.net%2Fdevices%2Fdevice1&sig=****&se=1592830262",

        //https://itnext.io/establishing-a-connection-to-azure-iot-hub-using-an-mqtt-client-with-nanoframework-d9c2e1b4ebbe

        //INFO https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-dev-guide-sas?tabs=node#authenticating-a-device-to-iot-hub
        public static string username = $"{hubName}.azure-devices.net/{deviceId}/?api-version=2021-04-12";

        //REMARK Generate SAS token for IoT Hub with VS Code
        public static string password = sasToken; //$"<YOUR-SAS-TOKEN>";

        //INFO https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-c2d
        public static string subTopic = $"\"devices/{deviceId}/messages/devicebound/#\"";

        //INFO https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-d2c
        public static string pubTopic = $"\"devices/{deviceId}/messages/events/\"";

    
        public static  string[] NTP_SERVERS = new string[] { "pool.ntp.org", "time.nist.gov" };

        public static DateTime GetNitTime()
        {
            var client = new TcpClient("time.nist.gov", 13); //Alt pool.ntp.org
            using (var streamReader = new StreamReader(client.GetStream()))
            {
                var response = streamReader.ReadToEnd();
                var utcDateTimeString = response.Substring(7, 17);
                var localDateTime = DateTime.ParseExact(utcDateTimeString, "yy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                return localDateTime;
            }
        }

    }
}
