using System;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ABUS.Common;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;


namespace ABUS.DeviceConnectivity.SimulatedDevice.DeviceProvisioning
{
    internal class DeviceProvisioningService : IDeviceProvisioningService
    {
        private readonly ICertificateFactory _certificateFactory;
        private readonly X509Certificate2Collection _certificate2Collection;
        protected internal IResourceInfoProvider ResourceInfoProvider;
        private const string GlobalDeviceEndpoint = "global.azure-devices-provisioning.net";

        
        public DeviceProvisioningService(IResourceInfoProvider resourceInfoProvider, ICertificateFactory certificateFactory)
        {
            _certificateFactory = certificateFactory;
            ResourceInfoProvider = resourceInfoProvider;
            _certificate2Collection = new X509Certificate2Collection { _certificateFactory.IntermediateCertificate };
        }

        public X509Certificate2 GetDeviceCertificate()
        {
            return _certificateFactory.DeviceCertificate;
        }

        public async Task<IoTHubConnectionParameters> CreateAndRegister()
        {
            X509Certificate2 certificate = _certificateFactory.CreateSignedDeviceCertificate(Guid.NewGuid());

            using (var security = new SecurityProviderX509Certificate(certificate, _certificate2Collection))
            {
                // Select one of the available transports:
                // To optimize for size, reference only the protocols used by your application.
                using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
                {
                    var idScope = ResourceInfoProvider.GetResourceInfoAsync("dpsIDScope").Result;
                    var provisioningDeviceClient = ProvisioningDeviceClient.Create(GlobalDeviceEndpoint, idScope, security, transport);
                    return await Register(provisioningDeviceClient, security);
                }
            }
        }

        private async Task<IoTHubConnectionParameters> Register(ProvisioningDeviceClient provisioningDeviceClient, SecurityProvider security)
        {
            Console.WriteLine($"RegistrationID = {security.GetRegistrationID()}");
            VerifyRegistrationIdFormat(security.GetRegistrationID());

            Console.Write("ProvisioningClient RegisterAsync . . . ");
            var result = await provisioningDeviceClient.RegisterAsync().ConfigureAwait(false);

            Console.WriteLine($"{result.Status}");
            Console.WriteLine($"ProvisioningClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

            IAuthenticationMethod authenticationMethod;
            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                return null;
            }

            if (security is SecurityProviderTpm)
            {
                Console.WriteLine("Creating TPM DeviceClient authentication.");
                authenticationMethod = new DeviceAuthenticationWithTpm(result.DeviceId, security as SecurityProviderTpm);
                return new IoTHubConnectionParameters(result.AssignedHub, authenticationMethod);
            }
            if (security is SecurityProviderX509)
            {
                Console.WriteLine("Creating X509 DeviceClient authentication.");
                authenticationMethod = new DeviceAuthenticationWithX509Certificate(result.DeviceId, (security as SecurityProviderX509).GetAuthenticationCertificate());
                return new IoTHubConnectionParameters(result.AssignedHub, authenticationMethod);
            }

            throw new NotSupportedException("Unknown authentication type.");
        }

        private void VerifyRegistrationIdFormat(string v)
        {
            var r = new Regex("^[a-z0-9-]*$");
            if (!r.IsMatch(v))
            {
                throw new FormatException("Invalid registrationId: The registration ID is alphanumeric, lowercase, and may contain hyphens");
            }
        }
    }
}
