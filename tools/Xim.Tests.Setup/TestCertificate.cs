using System;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Xim.Tests.Setup.Cng;

namespace Xim.Tests.Setup
{
    public static class TestCertificate
    {
        private const string CertificateName = "Xim Test Certificate";
        private const string CertificatePass = "Xim";
        private const string OidServerAuthentication = "1.3.6.1.5.5.7.3.1";

        private static X509Certificate2 Create()
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(IPAddress.Loopback);
            sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddDnsName("127.0.0.1");
            sanBuilder.AddDnsName(Environment.MachineName);

            var distinguishedName = new X500DistinguishedName($"CN={CertificateName}");

            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

                request.CertificateExtensions.Add(
                   new X509EnhancedKeyUsageExtension(
                       new OidCollection { new Oid(OidServerAuthentication) }, false));

                request.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddYears(100)));
                certificate.FriendlyName = CertificateName;

                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, CertificatePass), CertificatePass, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
            }
        }

        private static void Register(X509Certificate2 certificate, StoreName storeName, Func<X509Certificate2, X509Certificate2> onAdd = null)
        {
            using (var store = new X509Store(storeName, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                try
                {
                    store.Add(certificate);
                    onAdd?.Invoke(certificate);
                }
                finally
                {
                    store.Close();
                }
            }
        }

        public static void SafeRegister()
        {
            if (Find(StoreName.My) == null)
            {
                Register();
            }
            else
            {
                Console.WriteLine("Certificate already registered");
            }
        }

        public static X509Certificate2 Register()
        {
            var certificate = Create();
            Console.WriteLine("Adding certificate to user store");
            Register(certificate, StoreName.My, SetupPrivateKeyAccess);
            Console.WriteLine("Adding certificate to ca root store");
            Register(certificate, StoreName.Root);
            Console.WriteLine("Certificate registered");
            return certificate;
        }

        private static X509Certificate2 SetupPrivateKeyAccess(X509Certificate2 certificate)
        {
            const string usersGroupName = "Users";

            var sid = new NTAccount(usersGroupName).Translate(typeof(SecurityIdentifier));

            Console.WriteLine($"Adding certificate read access for {Environment.MachineName}\\{usersGroupName} ({sid})");
            var privateKey = certificate.GetRSAPrivateKey();
            if (privateKey is RSACryptoServiceProvider rsaCsp)
            {
                var cspParams = new CspParameters(rsaCsp.CspKeyContainerInfo.ProviderType, rsaCsp.CspKeyContainerInfo.ProviderName, rsaCsp.CspKeyContainerInfo.KeyContainerName)
                {
                    Flags = CspProviderFlags.UseExistingKey | CspProviderFlags.UseMachineKeyStore,
                    CryptoKeySecurity = rsaCsp.CspKeyContainerInfo.CryptoKeySecurity
                };

                cspParams.CryptoKeySecurity.AddAccessRule(new CryptoKeyAccessRule(sid, CryptoKeyRights.GenericRead, AccessControlType.Allow));

                using (new RSACryptoServiceProvider(cspParams))
                {
                    Console.WriteLine("Access granted (csp)");
                }
            }
            else if (privateKey is RSACng rsaCng)
            {
                var uniqueKeyName = rsaCng.Key.UniqueName;
                var keyFileName = CngUtils.GetContainerPath(uniqueKeyName);
                CngUtils.AddAcl(keyFileName, sid);
                Console.WriteLine("Access granted (cng)");
            }
            else
            {
                Console.WriteLine("Private key RSACryptoServiceProvider not found");
            }

            return certificate;
        }

        private static X509Certificate2 Find(StoreName storeName)
        {
            using (var store = new X509Store(storeName, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                try
                {
                    foreach (var certificate in store.Certificates)
                    {
                        if (CertificateName.Equals(certificate.FriendlyName))
                        {
                            return certificate;
                        }
                    }
                    return null;
                }
                finally
                {
                    store.Close();
                }
            }
        }
    }
}
