using Meadow;
using Meadow.Devices;

using System;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using System.Text;
using Meadow.Hardware;

// https://learn.microsoft.com/en-us/azure/iot/iot-mqtt-connect-to-iot-hub

using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Serilog;
using MQTTnet.Adapter;
using MQTTnet.Client.Receiving;
using System.Security.Authentication;
using System.Globalization;
using System.Security.Cryptography;
using System.Web;
//using MQTTnet.Client.Extensions.AzureIoT;
using MQTTnet.Client;
using Microsoft.Azure.Devices.Client;
//https://dev.to/eduardojuliao/basic-mqtt-with-c-1f88

//using System.Security.Authentication;

namespace MeadowApplication3
{
    // Change F7CoreComputeV2 to F7FeatherV2 (or F7FeatherV1) for Feather boards
    public class MeadowApp : App<F7CoreComputeV2>
    {
        private static IManagedMqttClient _mqttClient;
        private static bool Connected = false;

        public override Task Run()
        {
            Resolver.Log.Info("Run...");
            Resolver.Log.Info("Hello, Meadow Core-Compute!");
            return base.Run();
        }

        const string AZ_IOT_HUB_CLIENT_C2D_SUBSCRIBE_TOPIC = "\"devices/+/messages/devicebound/#\"";

        private static async Task SendDeviceToCloudMessagesAsync()
        {
            Resolver.Log.Info("Sending Telemetry");
            if (Secrets.UseMQTTnetAPI)
            {
                while (true)
                {
                    // Using mQTTnet API
                    var applicationMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(Secrets.pubTopic)
                        .WithPayload("{"+$"\"Count\":{_mqttClient.PendingApplicationMessagesCount}" +"}")
                        .Build();
                    try
                    {
                        var res = await _mqttClient.PublishAsync(applicationMessage);

                        Resolver.Log.Info($"Result: {res.ReasonCode} {res.ReasonString}");
                        Resolver.Log.Info($"Msg Count: {_mqttClient.PendingApplicationMessagesCount}");
                        Thread.Sleep(Secrets.TelemetryPeriod);
                        Resolver.Log.Info($"Msg Count now: {_mqttClient.PendingApplicationMessagesCount}");
                    }
                    catch (Exception ex)
                    {
                        Resolver.Log.Info($"Error: {ex.Message}");
                        Thread.Sleep(Secrets.TelemetryPeriod);
                    }
                }
            }
            else
            {
                // Using Microsoft.Azure.Devices.Client API
                // Initial telemetry values
                double minTemperature = 20;
                double  minHumidity = 60;
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

                    string json = JsonConvert.SerializeObject(message);
                    try
                    {
                        var res = await _mqttClient.PublishAsync(json);

                         Resolver.Log.Info($"Result: {res.ReasonCode} {res.ReasonString}");
                         Resolver.Log.Info($"Msg Count: {_mqttClient.PendingApplicationMessagesCount}");
                        Thread.Sleep(Secrets.TelemetryPeriod);
                         Resolver.Log.Info($"Msg Count now: {_mqttClient.PendingApplicationMessagesCount}");
                    }
                    catch (Exception ex)
                    {
                         Resolver.Log.Info($"Error: {ex.Message}");
                        Thread.Sleep(Secrets.TelemetryPeriod);
                    }
                }
            }
        }

        private  async void NetworkConnected(INetworkAdapter sender, NetworkConnectionEventArgs args)
        {
            Resolver.Log.Info("WiFi Connected. Starting MQTT...");
            Connected = false;

            // https://dev.to/eduardojuliao/basic-mqtt-with-c-1f88
            try
            {
                string SasToken = CreateTSasTokenfromDevicePrimaryKeyV2($"{Secrets.HubName}.azure-devices.net/devices/{Secrets.DeviceId}", "Primary", Secrets.DevicePrimaryKey);

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
                    byte[] payload = a.ApplicationMessage.Payload;
                    var msg = System.Text.Encoding.Default.GetString(payload);
                     Resolver.Log.Info($"Message recieved: {msg}");
                });

                // Starts a connection with the Broker
                await _mqttClient.StartAsync(options);

                Resolver.Log.Info("MQTT Client Started...");

                await _mqttClient.SubscribeAsync(Secrets.subTopic);

                bool isstarted = _mqttClient.IsStarted;
                Resolver.Log.Info($"Is started: {isstarted}");

            } catch (Exception ex)
            {
                Resolver.Log.Info($"MQTT error: {ex.Message}");
                return;
            }
            int count = 0;
            while (!Connected)
            {
                Thread.Sleep(3333);
                Resolver.Log.Info($"Waiting for connection: {++count}");
            }

            await SendDeviceToCloudMessagesAsync();
        }

        /*
         * Powershell Command to get SAS Token
         az iot hub generate-sas-token --hub-name HUBNAME --device-id DEVICEID --resource-group AZURERESOURCEGROUP --login IOTHUBCONNECTIONSTRIN
         Also -du optional parameter for duration: Valid token duration in seconds.  Default: 3600, 1 hr
         */
        //From https://stackoverflow.com/questions/50814994/how-to-generate-sas-token-for-secure-connection-to-azure-iot-hub
        private static string CreateTSasTokenfromDevicePrimaryKeyV2(string resourceUri, string keyName, string key, int durationTotalSecs = 0, int durationDays = 0, int durationHours = 0, int durationMins = 0)
        {
            //This fails on Meadow so jsut return a value from Powershell
            return "SharedAccessSignature sr=ozz2hub.azure-devices.net%2Fdevices%2Fozz2dev&sig=gbNInMk4HZhgVGRzXHOry73u59aQ6zVAMdGddP1hK5k%3D&se=1696683918";
            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int duration = 60 * 60 * 24 * 7;
            //if (durationTotalSecs > 0)
            //    duration = durationTotalSecs;
            //else if (durationDays > 0)
            //    duration = durationDays * 24 * 60 * 60;
            //else if (durationHours > 0)
            //    duration = durationHours * 60 * 60;
            //else if (durationMins > 0)
            //    duration = durationMins * 60;
            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + duration);
            string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
            HMACSHA256 hmac = new HMACSHA256(Convert.FromBase64String(key));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = String.Format(CultureInfo.InvariantCulture,
                "SharedAccessSignature sr={0}&sig={1}&se={2}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, keyName);
            //"SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, keyName);
            return sasToken;
        }

        public void StartWiFi()
        {
            var task = Task.Run(async () =>
            {
                var wifi = Device.NetworkAdapters.Primary<IWiFiNetworkAdapter>();
                wifi.NetworkConnected += NetworkConnected;
                Resolver.Log.Info("Connecting WiFi...");
                await wifi.Connect(Secrets.WIFI_NAME, Secrets.WIFI_PASSWORD);
            });;
        }

        public override Task Initialize()
        {
            Resolver.Log.Info("Initialize...");
            Resolver.Log.Info("IoT Hub using MQTTnet.\n");

            StartWiFi();
            return base.Initialize();
        }

        public static void OnConnected(MqttClientConnectedEventArgs obj)
        {         
            Resolver.Log.Info("Successfully connected.");
            Connected = true;
        }

        public static void OnConnectingFailed(ManagedProcessFailedEventArgs obj)
        {
            Resolver.Log.Info("Couldn't connect to broker.");
        }

        public static void OnDisconnected(MqttClientDisconnectedEventArgs obj)
        {
            _mqttClient?.Dispose();
            Resolver.Log.Info("Successfully disconnected.");
        }
    }
}