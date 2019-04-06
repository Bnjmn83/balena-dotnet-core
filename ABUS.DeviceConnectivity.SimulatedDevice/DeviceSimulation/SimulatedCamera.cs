using System.Net;
using System.Net.NetworkInformation;
using ABUS.DeviceConnectivity.Messages.Camera;
using ABUS.DeviceConnectivity.SimulatedDevice.DeviceProvisioning;

namespace ABUS.DeviceConnectivity.SimulatedDevice.DeviceSimulation
{
    public class SimulatedCamera : SimulatedDevice
    {
        public CameraSettingsMessage CameraSettingsMessage { get; set; }

        public SimulatedCamera(IoTHubConnectionParameters ioTHubConnection, string deviceName) : base(ioTHubConnection)
        {
            CameraSettingsMessage = new CameraSettingsMessage
            {
                CameraSettings = new CameraSettings(Dns.GetHostName(),
                    "1.0.1",
                    "",
                    NetworkInterface.GetAllNetworkInterfaces().GetNetworkInterface().GetNetworkSettings(),
                    80,
                    554,
                    1000,
                    0,
                    10,
                    deviceName)
            };
        }
    }
}
