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


