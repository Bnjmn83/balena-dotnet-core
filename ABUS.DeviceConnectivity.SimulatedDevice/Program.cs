using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ABUS.Common;
using ABUS.Common.Azure;
using ABUS.DeviceConnectivity.SimulatedDevice.DeviceProvisioning;
using ABUS.DeviceConnectivity.SimulatedDevice.DeviceSimulation;
using Autofac;
using Autofac.Core.Activators.Reflection;
using Microsoft.Azure.Devices.Client;

namespace ABUS.DeviceConnectivity.SimulatedDevice
{
    public class LocalStorageCredentialsDto
    {
        public string AssignedHub { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
    }

    public static class Program
    {
        private static IContainer _container;
        private static int _interval;
        private static int _persistIotHubCredentials;
        public static IDeviceProvisioningService DeviceProvisioningService { get; set; }
        private const string FileSystemCredentialsPath = @"./credentials.txt";
        private const string FileSystemCertificatePath = @"./certificate.pfx";

        private static readonly string[] DeviceNameList =
        {
            "Gartentor", "Haupteingang", "Eingang Terrasse", "Garage", "Wohnungstuer", "Poolhaus",
            "Hinterhof", "Hoftor", "Einfahrt Tor", "Lieferanteneingang", "Eingangsbereich", "Kellerraum",
            "Warenannahme", "Nebengebaeude", "Nebengebäude"
        };

        private static string _deviceName;

        public static void Main(string[] args)
        {
            Console.WriteLine("Virtual Device startet...");
            var name = Utils.GetEnvironmentVariable<string>("keyvault_name", null);
            var clientId = Utils.GetEnvironmentVariable<string>("keyvault_clientid", null);
            var clientSecret = Utils.GetEnvironmentVariable<string>("keyvault_clientsecret", null);
            var signingCertificate = Utils.GetEnvironmentVariable<string>("signing_certificate", 
                "IntermediateSimulationDPSCertificate");
            _interval = Utils.GetEnvironmentVariable("virtualclient_interval", 30);
            _persistIotHubCredentials = Utils.GetEnvironmentVariable("persist_iothub_credentials", 0);

            var builder = new ContainerBuilder();
            builder.Register(x => new KeyVaultSettings(name, clientId, clientSecret)).SingleInstance();
            builder.Register(x => new CertificateFactory(x.Resolve<IResourceInfoProvider>(), signingCertificate))
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<KeyVaultConfigurationProvider>().AsImplementedInterfaces()
                .UsingConstructor(new MatchingSignatureConstructorSelector(typeof(KeyVaultSettings)))
                .SingleInstance();
            builder.RegisterType<DeviceProvisioningService>().AsImplementedInterfaces();
            _container = builder.Build();

            MainAsync(args, CancellationToken.None).GetAwaiter().GetResult();

            Console.WriteLine("Virtual Device ended...");
        }

        private static IoTHubConnectionParameters FindCredentialsLocally()
        {
            if (!File.Exists(FileSystemCredentialsPath) || !File.Exists(FileSystemCertificatePath))
            {
                return null;
            }

            try
            {
                // Read credentials from file
                var data = File.ReadAllText(FileSystemCredentialsPath);
                var credentials = Newtonsoft.Json.JsonConvert.DeserializeObject<LocalStorageCredentialsDto>(data);

                // Load certificate from file
                X509Certificate2 cert = new X509Certificate2(
                    FileSystemCertificatePath,
                    "",
                    X509KeyStorageFlags.PersistKeySet);

                _deviceName = credentials.DeviceName;

                // Create authentication instance
                var authenticationMethod = new DeviceAuthenticationWithX509Certificate(credentials.DeviceId, cert);
                return new IoTHubConnectionParameters(credentials.AssignedHub, authenticationMethod);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        private static void StoreCredentialsLocally(IoTHubConnectionParameters param)
        {
            try
            {
                // Store credentials in local file
                var auth = param.AuthenticationMethod as DeviceAuthenticationWithX509Certificate;
                LocalStorageCredentialsDto credentials = new LocalStorageCredentialsDto()
                {
                    AssignedHub = param.AssignedHub,
                    DeviceId = auth.DeviceId,
                    DeviceName = _deviceName
                };

                var data = Newtonsoft.Json.JsonConvert.SerializeObject(credentials);
                File.WriteAllText(FileSystemCredentialsPath, data);

                // Export certificate to local file
                var certificate = DeviceProvisioningService.GetDeviceCertificate();
                byte[] certData = certificate.Export(X509ContentType.Pfx);
                File.WriteAllBytes(FileSystemCertificatePath, certData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static async Task MainAsync(string[] args, CancellationToken cancellationToken)
        {
            // create and register device
            DeviceProvisioningService = _container.Resolve<IDeviceProvisioningService>();

            IoTHubConnectionParameters ioTHubConnection = null;

            // Check if local credential persistence is enabled; if so read 'em
            if (Convert.ToBoolean(_persistIotHubCredentials))
            {
                Console.WriteLine("Check for local credentials");
                ioTHubConnection = FindCredentialsLocally();

                if (ioTHubConnection != null)
                {
                    Console.WriteLine("Using local credentials");
                }
            }

            // No local credentials found, create and register new virtual device
            if (ioTHubConnection == null)
            {
                try
                {
                    _deviceName = GetRandomDeviceName();
                }
                catch
                {
                    _deviceName = "Gartentor 3";
                }
                
                Console.WriteLine("Create and register new device");
                ioTHubConnection = await DeviceProvisioningService.CreateAndRegister();

                // Make sure to store credentials locally if requested
                if (Convert.ToBoolean(_persistIotHubCredentials))
                {
                    Console.WriteLine("Store credentials locally");
                    StoreCredentialsLocally(ioTHubConnection);
                }
            }

            // Check for given name via env var
            var envDeviceName = Utils.GetEnvironmentVariable("device_name", "");

            if (!string.IsNullOrEmpty(envDeviceName))
            {
                _deviceName = envDeviceName;
            }

            // send message by interval
            using (var simulatedCamera = new SimulatedCamera(ioTHubConnection, _deviceName))
            {
                // ReSharper cannot detect that the flow of both actions within the parallel execution
                // are wrapped in a while loop and hence will warn us. Suppress it by wrapping it in 
                // special comments.
                // ReSharper disable AccessToDisposedClosure
                Parallel.Invoke(
                    () => simulatedCamera.RandomAlert(simulatedCamera.SendMessage),
                    () =>
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                // For unix hosts only
                                var seconds = Uptime();
                                simulatedCamera.CameraSettingsMessage.CameraSettings.UpTimeSecond = seconds;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }

                            simulatedCamera.SendMessage(simulatedCamera.CameraSettingsMessage).GetAwaiter().GetResult();
                            Task.Delay(_interval * 1000, cancellationToken).GetAwaiter().GetResult();
                        }
                    }
                );
                // ReSharper restore AccessToDisposedClosure
            }
        }

        private static long Uptime()
        {
            string command = "stat /proc/1";
            string result = ExecBashCmd(command);

            Regex regex = new Regex(@"Modify: ([0-9-:+. ]*)");
            Match match = regex.Match(result);

            if (match.Success)
            {
                var unixTime = DateTimeOffset.Now.ToUnixTimeSeconds() - DateTimeOffset.Parse(match.Groups[1].Value).ToUnixTimeSeconds();
                Console.WriteLine("Uptime´: " +  unixTime);
                return unixTime;
            }

            return 1000;
        }

        private static string GetRandomDeviceName()
        {
            string command = "head -c 1 /dev/urandom | od -t dI -A n | awk '{print $1}'";
            string result = ExecBashCmd(command);

            var random = new Random(Convert.ToInt16(result));
            Console.WriteLine("random: " + Convert.ToInt16(result));

            return DeviceNameList[random.Next(0, DeviceNameList.Length)] + " " + random.Next(1, 10);
        }

        private static string ExecBashCmd(string command)
        {
            string result = "";

            using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
            {
                proc.StartInfo.FileName = "/bin/bash";
                proc.StartInfo.Arguments = "-c \" " + command + " \"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.Start();
                result += proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
            }

            return result;
        }
    }

}
