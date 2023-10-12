using System;
using System.Text.Json.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Info;

using System.Windows;


using Newtonsoft.Json;
using System.Configuration;

// https://learn.microsoft.com/en-us/azure/iot/iot-mqtt-connect-to-iot-hub

using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Adapter;
using MQTTnet.Client.Receiving;
using System.Security.Authentication;
using System.Globalization;
using System.Security.Cryptography;
using System.Web;
//using MQTTnet.Client.Extensions.AzureIoT;
using MQTTnet.Client;
using Microsoft.Azure.Devices.Client;
using static Microsoft.Azure.Amqp.Serialization.SerializableType;
using System.Runtime.Intrinsics.X86;
using System.Net;
using System.IO;
using System.Reflection;
//https://dev.to/eduardojuliao/basic-mqtt-with-c-1f88

namespace ConsoleMqttApplicastion
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Get SasToken and copy to ClipBoard");
            string SasToken = CreateTSasTokenfromDevicePrimaryKeyV2($"{Secrets.HubName}.azure-devices.net/devices/{Secrets.DeviceId}", "Primary", Secrets.DevicePrimaryKey, 0, 1);
            TextCopy.ClipboardService.SetText(SasToken);

            Console.WriteLine("Done");
        }

        static int EncodeMode = 2;
        public static string Encode(string msg)
        { 
            string str  = "";
            switch (EncodeMode)
            {
                case 0:
                    str = HttpUtility.UrlEncode(msg); //Works: Token generated (Crashes with Meadow app)
                    break;
                case 1:
                    str =Uri.EscapeUriString(msg);    //Invalid Token generated
                    break;
                case 2:
                    str =Uri.EscapeDataString(msg);   //Works: Token generated (But fails to connect in Meadow App)
                    break;
                case 3:
                    str = WebUtility.HtmlEncode(msg); //Invalid Token generated
                    break;
            }
            return str;
        }
        /*
         * Powershell Command to get SAS Token
         az iot hub generate-sas-token --hub-name HUBNAME --device-id DEVICEID --resource-group AZURERESOURCEGROUP --login IOTHUBCONNECTIONSTRIN
         Also -du optional parameter for duration: Valid token duration in seconds.  Default: 3600, 1 hr
         */
        //From https://stackoverflow.com/questions/50814994/how-to-generate-sas-token-for-secure-connection-to-azure-iot-hub
        private static string CreateTSasTokenfromDevicePrimaryKeyV2(string resourceUri, string keyName, string key, int durationTotalSecs = 0, int durationDays = 0, int durationHours = 0, int durationMins = 0)
        {
 
            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int duration = 60 * 60 * 24 * 7;
            if (durationTotalSecs > 0)
                duration = durationTotalSecs;
            else if (durationDays > 0)
                duration = durationDays * 24 * 60 * 60;
            else if (durationHours > 0)
                duration = durationHours * 60 * 60;
            else if (durationMins > 0)
                duration = durationMins * 60;
            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + duration);
            string stringToSign = Encode(resourceUri) + "\n" + expiry;
            HMACSHA256 hmac = new HMACSHA256(Convert.FromBase64String(key));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            Console.WriteLine(signature);
            string nmx = Encode(signature);
            var sasToken = String.Format(CultureInfo.InvariantCulture,
                "SharedAccessSignature sr={0}&sig={1}&se={2}", Encode(resourceUri), Encode(signature), expiry, keyName); ;
            //"SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, keyName);
           Console.WriteLine(sasToken);;
            return sasToken;
        }

        private static IManagedMqttClient _mqttClient;
        private static bool Connected = false;
        private static async Task SendDeviceToCloudMessagesAsync()
        {
            Console.WriteLine("Sending Telemetry");
            if (Secrets.UseMQTTnetAPI)
            {
                while (true)
                {
                    // Using mQTTnet API
                    var applicationMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(Secrets.pubTopic)
                        .WithPayload("{" + $"\"Count\":{_mqttClient.PendingApplicationMessagesCount}" + "}")
                        .Build();
                    try
                    {
                        var res = await _mqttClient.PublishAsync(applicationMessage);

                        Console.WriteLine($"Result: {res.ReasonCode} {res.ReasonString}");
                        Console.WriteLine($"Msg Count: {_mqttClient.PendingApplicationMessagesCount}");
                        Thread.Sleep(Secrets.TelemetryPeriod);
                        Console.WriteLine($"Msg Count now: {_mqttClient.PendingApplicationMessagesCount}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        Thread.Sleep(Secrets.TelemetryPeriod);
                    }

                }
            }
            else
            {
                // Using Microsoft.Azure.Devices.Client API
                // Initial telemetry values
                double minTemperature = 20;
                double minHumidity = 60;
                Random rand = new Random();
                while (true)
                {
                    double currentTemperature = minTemperature + rand.NextDouble() * 15;
                    double currentHumidity = minHumidity + rand.NextDouble() * 20;

                    // Create JSON message
                    var telemetryDataPoint = new
                    {
                        temperature = currentTemperature,
                        humidity = currentHumidity
                    };
                    var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                    Message message = new Message(Encoding.ASCII.GetBytes(messageString));

                    // Add a custom application property to the message.
                    // An IoT hub can filter on these properties without access to the message body.
                    message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                    //Use devices/{device-id}/modules/{module-id}/messages/events/
                    //
                    string json = JsonConvert.SerializeObject(new { message = "Heyo :)", sent = DateTimeOffset.UtcNow });
                    var res = await _mqttClient.PublishAsync("devices/ozz2dev/messages/events", json);
                    Console.WriteLine($"Result: {res.ReasonCode} {res.ReasonString}");
                    Console.WriteLine($"MsgQ Count: {_mqttClient.PendingApplicationMessagesCount}");
                    await Task.Delay(Secrets.TelemetryPeriod);
                    Console.WriteLine($"MsgQ Count now: {_mqttClient.PendingApplicationMessagesCount}");
                }
            }
        }

        private static async Task MqttConnect()
        {
            Console.WriteLine("Starting MQTT...");
            Connected = false;

            // https://dev.to/eduardojuliao/basic-mqtt-with-c-1f88
            try
            {
                string SasToken = CreateTSasTokenfromDevicePrimaryKeyV2($"{Secrets.HubName}.azure-devices.net/devices/{Secrets.DeviceId}", "Primary", Secrets.DevicePrimaryKey,0,1);

                MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
                                        .WithClientId(Secrets.DeviceId)
                                        .WithTls(new MqttClientOptionsBuilderTlsParameters()
                                        {
                                            UseTls = true, 
                                            SslProtocol = SslProtocols.Tls
                                        })
                                        .WithCredentials(Secrets.username, SasToken)
                                        .WithKeepAlivePeriod(TimeSpan.FromSeconds(3600))
                                        .WithCleanSession(true)
                                        //.WithAuthentication(method,data)
                                        .WithTcpServer(Secrets.IOT_CONFIG_IOTHUB_FQDN, Secrets.MqttPort);

                // Create client options objects
                ManagedMqttClientOptions options = new ManagedMqttClientOptionsBuilder()
                                        .WithAutoReconnectDelay(TimeSpan.FromSeconds(60))
                                        .WithClientOptions(builder.Build())
                                        .Build();

                // Creates the client object
                _mqttClient = new MqttFactory().CreateManagedMqttClient();


                // Set up handlers
                _mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnConnected);
                _mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnDisconnected);
                _mqttClient.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(OnConnectingFailed);

                _mqttClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(a => {
                    byte[] payload= a.ApplicationMessage.Payload;
                    var msg = System.Text.Encoding.Default.GetString(payload);
                    Console.WriteLine($"Message recieved: {msg}");
                });

                // Starts a connection with the Broker
                await _mqttClient.StartAsync(options);

                Console.WriteLine("MQTT Client Started...");

                await _mqttClient.SubscribeAsync(Secrets.subTopic);

                bool isstarted = _mqttClient.IsStarted;
                Console.WriteLine($"Is started: {isstarted}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT error: {ex.Message}");
                Console.WriteLine("Press ]Enter] to exit");
                Console.ReadLine();
                return;
            }
            int count = 0;
            while (!Connected)
            {
                Thread.Sleep(3333);
                Console.WriteLine($"Waiting for connection: {++count}");
            };
            await SendDeviceToCloudMessagesAsync();
        }



        public static void OnConnected(MqttClientConnectedEventArgs obj)
        {
            Console.WriteLine("Successfully connected.");
            Connected = true;
        }

        public static void OnConnectingFailed(ManagedProcessFailedEventArgs obj)
        {
            Console.WriteLine("Couldn't connect to broker.");
        }

        public static void OnDisconnected(MqttClientDisconnectedEventArgs obj)
        {
            _mqttClient?.Dispose();
            Console.WriteLine("Successfully disconnected.");
        }

    }
}
