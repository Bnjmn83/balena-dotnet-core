using Microsoft.Azure.Devices.Client;

namespace ABUS.DeviceConnectivity.SimulatedDevice.DeviceProvisioning
{
    public class IoTHubConnectionParameters
    {
        public IoTHubConnectionParameters(string assignedHub, IAuthenticationMethod authenticationMethod)
        {
            AssignedHub = assignedHub;
            AuthenticationMethod = authenticationMethod;
        }

        public string AssignedHub { get; }
        public IAuthenticationMethod AuthenticationMethod { get; }
    }
}
