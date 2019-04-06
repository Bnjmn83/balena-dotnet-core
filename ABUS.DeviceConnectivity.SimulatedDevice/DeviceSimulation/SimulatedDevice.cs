using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ABUS.DeviceConnectivity.Messages.Camera;
using ABUS.DeviceConnectivity.SimulatedDevice.DeviceProvisioning;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace ABUS.DeviceConnectivity.SimulatedDevice.DeviceSimulation
{
    public class SimulatedDevice : IDisposable
    {
        private readonly DeviceClient _deviceClient;
        private readonly string _deviceId;

        private static readonly List<(AlertSeverity Severity, string EventName)> CameraEventList = new List<(AlertSeverity Severity, string EventName)>()
        {
            (AlertSeverity.Info, "Alarm_alarmIn"),
            (AlertSeverity.Info, "Alarm_alarmOut"),
            (AlertSeverity.Info, "Alarm_motionStart"),
            (AlertSeverity.Info, "Alarm_motionStop"),
            (AlertSeverity.Warning, "Alarm_defocusDetectionStart"),
            (AlertSeverity.Warning, "Alarm_defocusDetectionStop"),
            (AlertSeverity.Warning, "Alarm_sceneChangeDetectionStart"),
            (AlertSeverity.Warning, "Alarm_sceneChangeDetectionStop"),

            (AlertSeverity.Critical, "Exception_illlegealAccess"),
            (AlertSeverity.Critical, "Exception_hdError"),

            (AlertSeverity.Info, "Operation_devicePowerOn"),
            (AlertSeverity.Info, "Operation_devicePowerOff"),
            (AlertSeverity.Info, "Operation_localLogin"),
            (AlertSeverity.Info, "Operation_localLogOut"),
            (AlertSeverity.Info, "Operation_localCfgPara"),
            (AlertSeverity.Info, "Operation_remotePowerOff"),
            (AlertSeverity.Info, "Operation_remotePowerRecycle"),
            (AlertSeverity.Info, "Operation_remoteLogin"),
            (AlertSeverity.Info, "Operation_remoteLogout"),
            (AlertSeverity.Info, "Operation_remoteCfgPara")
        };

        

        public SimulatedDevice(IoTHubConnectionParameters ioTHubConnection)
        {
            _deviceClient = DeviceClient.Create(ioTHubConnection.AssignedHub, ioTHubConnection.AuthenticationMethod, TransportType.Amqp);

            var auth = ioTHubConnection.AuthenticationMethod as DeviceAuthenticationWithX509Certificate;
            _deviceId = auth.DeviceId;

            Console.WriteLine("DeviceClient OpenAsync.");
            _deviceClient.OpenAsync().GetAwaiter().GetResult();
        }

        public void RandomAlert(Func<AlertMessage, Task> sendMessage) 
        {
            var random = new Random(DateTimeOffset.Now.Millisecond);

            while (true)
            {
                var randomEventIdx = random.Next(0, CameraEventList.Count);
                if (random.Next(0, 60) == 4)
                {
                    Console.WriteLine($"Event {CameraEventList[randomEventIdx].EventName } with severity {CameraEventList[randomEventIdx].Severity } ");

                    sendMessage(new AlertMessage
                    {
                        Alerts = new List<Alert>
                        {
                            new Alert(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), CameraEventList[randomEventIdx].Severity, CameraEventList[randomEventIdx].EventName)
                        }
                    }).GetAwaiter().GetResult(); 
                }

                Thread.Sleep(1000);
            }
        }

        public async Task SendMessage<T>(T data) where T : BaseMessage
        {
            var jsonMessage = JsonConvert.SerializeObject(data);
//            var message = new Message(Encoding.ASCII.GetBytes(jsonMessage));
            var message = new Message(Encoding.UTF8.GetBytes(jsonMessage));
            message.Properties.Add("messageversion", data.MessageVersion.ToString());
            message.Properties.Add("messageformat", "json");
            message.Properties.Add("messagetype", typeof(T).AssemblyQualifiedName);
            Console.WriteLine($"Device { _deviceId } SendEventAsync { DateTimeOffset.UtcNow } -- { typeof(T).Name }");
            await _deviceClient.SendEventAsync(message).ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Console.WriteLine("DeviceClient CloseAsync.");
                _deviceClient.CloseAsync().GetAwaiter().GetResult();
                _deviceClient?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}