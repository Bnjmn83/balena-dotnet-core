using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace ABUS.DeviceConnectivity.SimulatedDevice.DeviceProvisioning
{
    public interface IDeviceProvisioningService
    {
        Task<IoTHubConnectionParameters> CreateAndRegister();
        X509Certificate2 GetDeviceCertificate();
    }
}