using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ABUS.Common;

namespace ABUS.DeviceConnectivity.SimulatedDevice.DeviceProvisioning
{
    public interface ICertificateFactory
    {
        X509Certificate2 CreateSignedDeviceCertificate(Guid deviceId);
        X509Certificate2 IntermediateCertificate { get; }
        X509Certificate2 DeviceCertificate { get; }
    }

    public class CertificateFactory : ICertificateFactory
    {
        protected internal IResourceInfoProvider ResourceInfoProvider;
        public X509Certificate2 IntermediateCertificate { get; }
        public X509Certificate2 DeviceCertificate { get; private set; }

        public CertificateFactory(X509Certificate2 intermediateCertificate)
        {
            IntermediateCertificate = intermediateCertificate;
        }

        public CertificateFactory(IResourceInfoProvider resourceInfoProvider, string certificateSecret)
        {
            ResourceInfoProvider = resourceInfoProvider;
            IntermediateCertificate = new X509Certificate2(resourceInfoProvider.GetCertificate(certificateSecret).Result);
        }

        /// <summary>
        /// Helper to print a cert to console and to the file system
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="name"></param>
        public static void PrintCertToConsoleAndFile(X509Certificate2 certificate, string name)
        {
            Console.WriteLine("Printing Certificate " + name);
            var csrResSb = new StringBuilder();
            csrResSb.AppendLine("-----BEGIN CERTIFICATE-----");
            csrResSb.AppendLine(Convert.ToBase64String(certificate.Export(X509ContentType.Cert),
                Base64FormattingOptions.InsertLineBreaks));
            csrResSb.AppendLine("-----END CERTIFICATE-----");
            var signedCert = csrResSb.ToString();
            File.WriteAllText(name, signedCert);
            Console.WriteLine(signedCert);
        }

        /// <summary>
        /// Create a self signed certificate that can be used to sign leaf certificates.
        /// </summary>
        /// <param name="commonName"></param>
        /// <returns></returns>
        public static X509Certificate2 CreateSelfSignedCertHelper(string commonName)
        {
            var rsa = RSA.Create(4096);
            var req = new CertificateRequest("cn=" + commonName, rsa,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            req.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

            req.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, false, 0, true));

            req.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.KeyCertSign,
                    false));

            req.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection
                    {
                        new Oid("1.3.6.1.5.5.7.3.2"), // TLS Client auth
                        new Oid("1.3.6.1.5.5.7.3.1") // TLS Server auth
                    },
                    false));

            return req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.Now.AddYears(2));
        }

        /// <summary>
        /// Create a certificate with the CN deviceId and sign it with the configured intermediate certificate.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public X509Certificate2 CreateSignedDeviceCertificate(Guid deviceId)
        {
            // Create Private Key
            using (RSA rsa = RSA.Create(4096))
            {
                //A client creates a certificate signing request.
                CertificateRequest req = new CertificateRequest(
                    new X500DistinguishedName("CN=" + deviceId),
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                req.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(false, false, 0, true));

                req.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                        false));

                req.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection
                        {
                            new Oid("1.3.6.1.5.5.7.3.2"), // TLS Client auth
                            new Oid("1.3.6.1.5.5.7.3.1") // TLS Server auth
                        },
                        false));

                req.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

                // Set the AuthorityKeyIdentifier. There is no built-in 
                // support, so it needs to be copied from the Subject Key 
                // Identifier of the signing certificate and massaged slightly.
                // AuthorityKeyIdentifier is "KeyID="
                // BM: This relies on the subject key extension to be in the first array element. It works here just because
                // it was added as a the first item when the cert was created. This is very error prone.
                var issuerSubjectKey = IntermediateCertificate.Extensions[0].RawData;
                var segment = new ArraySegment<byte>(issuerSubjectKey, 2, issuerSubjectKey.Length - 2);
                var authorityKeyIdentifier = new byte[segment.Count + 4];

                // These bytes define the "KeyID" part of the AuthorityKeyIdentifer
                authorityKeyIdentifier[0] = 0x30;
                authorityKeyIdentifier[1] = 0x16;
                authorityKeyIdentifier[2] = 0x80;
                authorityKeyIdentifier[3] = 0x14;
                segment.CopyTo(authorityKeyIdentifier, 4);

                // Now add it as extension .35 which is AuthorityKeyIdentifier
                req.CertificateExtensions.Add(new X509Extension("2.5.29.35", authorityKeyIdentifier, false));

                // DPS samples create certs with the device name as a SAN name 
                // in addition to the subject name
                var sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName(deviceId.ToString());
                var sanExtension = sanBuilder.Build();
                req.CertificateExtensions.Add(sanExtension);

                byte[] serial = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(serial);
                }

                // Create the signed certificate using the intermediate certificate
                X509Certificate2 cert = req.Create(
                    IntermediateCertificate,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddDays(100),
                    serial);

                DeviceCertificate = cert.CopyWithPrivateKey(rsa);

                // TODO: Needed?
                var certCollection = new X509Certificate2Collection(DeviceCertificate);
                byte[] rawCert = certCollection.Export(X509ContentType.Pkcs12);

                Console.WriteLine("Successfully created leaf Certificate for device " + deviceId.ToString());
                return new X509Certificate2(rawCert);
            }
        }
    }
}
