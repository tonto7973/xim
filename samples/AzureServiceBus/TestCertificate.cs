using System.Security.Cryptography.X509Certificates;

namespace AzureServiceBusSample
{
    internal static class TestCertificate
    {
        private const string CertificateName = "Xim Test Certificate";

        public static X509Certificate2 Find()
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.OpenExistingOnly);
            try
            {
                foreach (var certificate in store.Certificates)
                {
                    if (CertificateName.Equals(certificate.FriendlyName))
                        return certificate;
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
