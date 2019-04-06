using System;
using System.Net;

namespace ABUS.DeviceConnectivity.Messages.Camera
{
    public class NetworkSettings
    {
        private NetworkSettings()
        {
            // private constructor for reflection based deserialization
        }

        public NetworkSettings(string macAddress, string ipAddress, string subnetmask, string gateway, string dns)
        {
            MACAddress = macAddress;
            IPAddress = ValidateIPString(ipAddress);
            Subnetmask = ValidateIPString(subnetmask);
            Gateway = ValidateIPString(gateway);
            DNS = ValidateIPString(dns);
        }
        
        public string MACAddress { get; private set; }
        public string IPAddress { get; private set; }
        public string Subnetmask { get; private set; }
        public string Gateway { get; private set; }
        public string DNS { get; private set; }

        private string ValidateIPString(string ipAddress)
        {
            if (System.Net.IPAddress.TryParse(ipAddress, out IPAddress innerIPAddress))
            {
                return innerIPAddress.ToString();
            }

            throw new ArgumentException($"Given ip string {ipAddress} is not a valid IPAddress");
        }
    }
}
