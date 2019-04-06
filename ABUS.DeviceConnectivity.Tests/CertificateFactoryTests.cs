using ABUS.DeviceConnectivity.SimulatedDevice.DeviceProvisioning;
using System;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Xunit.Abstractions;


namespace ABUS.DeviceConnectivity.Tests
{
    /// <summary>
    /// The intention of this test implementation is to verify the signing procedure of the CertificateFactory
    /// class.
    /// For this process to function the user provides a self signed CA certificate that can be used to sign other certs.
    /// In this test scenario the CA cert is created in advance using a helper function CreateSelfSignedCertHelper.
    /// The self signed CA cert is then passed into the CertificateFactory which in turn returns a signed certificate.
    /// This certificate is then validated in this test scenario using the created self signed CA cert. An additional
    /// self signed CA cert is create to perform the same test routine with an expected negative outcome.
    /// </summary>
    public class CertificateFactoryTests
    {

        private readonly ITestOutputHelper _output;

        public CertificateFactoryTests(ITestOutputHelper output)
        {
            _output = output;
        }


        /// <summary>
        /// A routine to verify certificates in a chain.
        /// The first method uses the X509chain interface to build a chain and compare the thumbprints of the CA cert and the leaf cert.
        /// The second method checks for the Authority Key Identifier extension of the leaf which should hold the Subject Key Identifier of the CA cert.
        /// Note: chain.ChainElements holds both certificates in a certain order starting with the leaf at array position 0.
        /// </summary>
        /// <param name="primaryCertificate"></param>
        /// <param name="additionalCertificate"></param>
        /// <returns></returns>
        public bool VerifyCertificate(X509Certificate2 primaryCertificate, X509Certificate2 additionalCertificate)
        {
            var chain = new X509Chain();

            chain.ChainPolicy.ExtraStore.Add(primaryCertificate);

            // Set verification conditions
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;

            // Do the preliminary validation.
            var status1 = chain.Build(additionalCertificate);
            _output.WriteLine("Chain Build result: " + status1);
            _output.WriteLine("CA Cert thumbprint:" + primaryCertificate.Thumbprint);

            for (int i = 0; i < chain.ChainElements.Count; i++)
            {
                _output.WriteLine("Additional Cert thumbprints: " + chain.ChainElements[i].Certificate.Thumbprint);
            }

            // Check if both certs are linked via their thumbprints
            var status2 = chain.ChainElements[chain.ChainElements.Count - 1].Certificate.Thumbprint == primaryCertificate.Thumbprint;
            _output.WriteLine("Chain compare thumbprints result: " + status2);

            // Read the Authority Key Identifier extension from the leaf certificate;
            // it must match with the Subject Key Identifier of the CA cert.
            var x509ExtensionCollection = additionalCertificate.Extensions;
            string authKey = "";

            _output.WriteLine("Check Extensions of Leaf Cert:");
            foreach (var ext in x509ExtensionCollection)
            {
                if (ext.Oid.Value == "2.5.29.35")
                {
                    _output.WriteLine("Checking 2.5.29.35:");
                    // Need to put try/catch here since some certs might not be well formed and fail here.
                    try
                    {
                        var authKeyId = ext.Format(true);
                        _output.WriteLine(authKeyId);
                        var authKeyIdSplit = authKeyId.Split("=");
                        authKey = authKeyIdSplit[1].Trim(new Char[] { ' ', '\n', '\r', '\t' });
                        _output.WriteLine(authKey);
                        break;
                    }
                    catch
                    {
                        _output.WriteLine("Certificate extension Authority Key Identifier has a different format; check next option");
                    }

                    try
                    {
                        var authKeyId = ext.Format(true);
                        _output.WriteLine(authKeyId);
                        authKey = authKeyId.Remove(0, 6).Trim(new Char[] { ' ', '\n', '\r', '\t' });
                        _output.WriteLine(authKey);
                        break;
                    }
                    catch
                    {
                        _output.WriteLine("Certificate has wrong format; exit");
                        return false;
                    }
                }
            }

            x509ExtensionCollection = primaryCertificate.Extensions;
            string subjKey = "";

            _output.WriteLine("Check Extensions of CA cert:");
            // Read the Subject Key Identifier of the CA cert.
            foreach (var ext in x509ExtensionCollection)
            {
                if (ext.Oid.Value == "2.5.29.14")
                {
                    _output.WriteLine("Checking 2.5.29.14:");
                    subjKey = ext.Format(true).Trim(new Char[] { ' ', '\n', '\r', '\t' });
                    _output.WriteLine(subjKey);
                }
            }

            var status3 = String.Equals(authKey.Trim(), subjKey.Trim());
            _output.WriteLine("Compare authKey vs subjKey result: " + status3);
            
            // Final result
            return (status3 && status1 && status2);
        }

        [Fact]
        public void SignAndVerifyCertificate()
        {
            _output.WriteLine("Start Tests SignAndVerifyCertificate");
            // Create the self signed CA cert we will use to sign the leaf certificate
            _output.WriteLine("Create Self Signed CA Certificate");
            X509Certificate2 trustedCert = CertificateFactory.CreateSelfSignedCertHelper("trustedCert");
            CertificateFactory.PrintCertToConsoleAndFile(trustedCert, "selfsignedcert.pem");

            // Pass the self signed certificate into the factory
            _output.WriteLine("Create Leaf Certificate");
            CertificateFactory cerFactory = new CertificateFactory(trustedCert);
            X509Certificate2 leafCertificate = cerFactory.CreateSignedDeviceCertificate(Guid.NewGuid());
            CertificateFactory.PrintCertToConsoleAndFile(leafCertificate, "device.pem");

            // Validate the certificate chain
            _output.WriteLine("Validate Certificate using signer CA Cert:");
            Assert.True(VerifyCertificate(trustedCert, leafCertificate));

            // Create another self signed certificate and repeat the test; should not succeed
            _output.WriteLine("Create another Self Signed CA Certificate");
            X509Certificate2 notTrustedCert = CertificateFactory.CreateSelfSignedCertHelper("notTrustedCert");
            CertificateFactory.PrintCertToConsoleAndFile(leafCertificate, "untrustedselfsignedcert.pem");
            _output.WriteLine("Validate Certificate using new CA Cert:");
            Assert.False(VerifyCertificate(notTrustedCert, leafCertificate));
        }
    }
}
