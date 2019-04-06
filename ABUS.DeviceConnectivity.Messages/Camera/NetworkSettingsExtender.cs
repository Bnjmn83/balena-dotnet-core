using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ABUS.DeviceConnectivity.Messages.Camera
{
    public static class NetworkSettingsExtender
    {
        public static NetworkSettings GetNetworkSettings(this NetworkInterface networkInterface)
        {
            return new NetworkSettings(
                networkInterface.GetPhysicalAddress().ToString(),
                networkInterface.GetIpAddress().Address.ToString(),
                networkInterface.GetIpAddress().IPv4Mask.ToString(),
                networkInterface.GetDefaultGateway().Address.ToString(),
                networkInterface.GetDns().ToString());
        }

        public static UnicastIPAddressInformation GetIpAddress(this NetworkInterface networkInterface)
        {
            return networkInterface.GetIPProperties().UnicastAddresses.FirstOrDefault(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        }

        public static IPAddress GetDns(this NetworkInterface networkInterface)
        {
            return networkInterface.GetIPProperties().DnsAddresses.FirstOrDefault().MapToIPv4();
        }

        public static GatewayIPAddressInformation GetDefaultGateway(this NetworkInterface networkInterface)
        {
            return networkInterface.GetIPProperties().GatewayAddresses.FirstOrDefault(x => x.Address != null);
        }

        public static NetworkInterface GetNetworkInterface(this IEnumerable<NetworkInterface> networkInterfaces)
        {
            return networkInterfaces
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                            n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            n.GetIPProperties().GatewayAddresses.Any(x => x.Address.AddressFamily == AddressFamily.InterNetwork));
        }
    }
}
