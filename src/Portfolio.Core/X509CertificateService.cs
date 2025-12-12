using System.Security.Cryptography.X509Certificates;

namespace Portfolio.Core
{
    public interface IX509CertificateService
    {
        /// <summary>
        /// Returns the predefined certificate if found, otherwise null.
        /// </summary>
        X509Certificate2? GetPreloadedCertificate();

        /// <summary>
        /// Attempts to find a certificate by subject name and returns it or null.
        /// </summary>
        X509Certificate2? GetCertificateByName(string subjectName);
    }

    public class X509CertificateService : IX509CertificateService
    {
        // Predefined certificate subject/friendly name
        private const string PredefinedCertName = "TirsvadWebCert";

        public X509Certificate2? GetPreloadedCertificate()
        {
            return GetCertificateByName(PredefinedCertName);
        }

        public X509Certificate2? GetCertificateByName(string subjectName)
        {
            if (string.IsNullOrWhiteSpace(subjectName))
            {
                return null;
            }

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                var found = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, validOnly: false);
                if (found != null && found.Count > 0)
                {
                    return found[0];
                }
            }
            catch
            {
                // swallow exceptions and return null if unable to read certificate
            }

            return null;
        }
    }
}
