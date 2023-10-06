using System;
using System.Text.Json.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Info;


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
//https://dev.to/eduardojuliao/basic-mqtt-with-c-1f88

namespace ConsoleMqttApplicastion
{
    internal class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("MQTTnet to Azure IOT Hub Device Console App");
            MqttConnect().GetAwaiter();
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
                MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
                                        .WithClientId(Secrets.deviceId)
                                        .WithTls(new MqttClientOptionsBuilderTlsParameters()
                                        {
                                            UseTls = true, 
                                            SslProtocol = SslProtocols.Tls
                                        })
                                        .WithCredentials(Secrets.username, Secrets.password)
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
