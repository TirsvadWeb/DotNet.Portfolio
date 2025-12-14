using Portfolio.Core.Abstracts;

using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace Portfolio.Core.Services;

/// <summary>
/// Implementation of <see cref="IX509CertificateService"/> that retrieves X.509
/// certificates from the current user's certificate store.
/// </summary>
/// <remarks>
/// This class demonstrates the use of XML documentation inheritance using
/// <c>&lt;inheritdoc/&gt;</c> on implementing members. Use <c>&lt;inheritdoc/&gt;</c>
/// to inherit the interface's documentation so that the documentation is:
/// - DRY (Declared once on the interface),
/// - Consistent between contract and implementation, and
/// - Automatically merged by IDEs and documentation generators.
/// 
/// How to use <c>&lt;inheritdoc/&gt;</c>:
/// - Place <c>&lt;inheritdoc/&gt;</c> above the implementing member (method/property).
/// - Ensure the interface member contains the authoritative documentation.
/// 
/// Example:
/// <code>
/// var svc = new X509CertificateService();
/// var cert = svc.GetPreloadedCertificate();
/// if (cert != null)
/// {
///     // use certificate, for example to configure a TLS handler
/// }
/// 
/// // Or look up by subject name:
/// var named = svc.GetCertificateByName("TirsvadWebCert");
/// </code>
/// </remarks>
public class X509CertificateService : IX509CertificateService
{
    // Predefined certificate subject/friendly name
    private const string PredefinedCertName = "TirsvadWebCert";

    // Simple in-memory cache to avoid repeated expensive store enumerations for hot lookups
    private static readonly ConcurrentDictionary<string, X509Certificate2?> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public X509Certificate2? GetPreloadedCertificate()
    {
        return GetCertificateByName(PredefinedCertName);
    }

    /// <inheritdoc/>
    public X509Certificate2? GetCertificateByName(string subjectName)
    {
        if (string.IsNullOrWhiteSpace(subjectName))
        {
            return null;
        }

        // Check cache first
        if (_cache.TryGetValue(subjectName, out X509Certificate2? cached))
        {
            // Validate cached entry is still present in the store. Certificates
            // may be removed externally; returning a stale cached certificate
            // causes callers to observe certificates that no longer exist.
            if (cached != null)
            {
                try
                {
                    using X509Store validateStore = new(StoreName.My, StoreLocation.CurrentUser);
                    validateStore.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection found = validateStore.Certificates.Find(X509FindType.FindByThumbprint, cached.Thumbprint, validOnly: false);
                    if (found != null && found.Count > 0)
                    {
                        return new X509Certificate2(cached);
                    }
                    else
                    {
                        // Stale cache, remove and fall through to fresh lookup
                        _cache.TryRemove(subjectName, out _);
                    }
                }
                catch
                {
                    // If we cannot access the store for validation, fall back to
                    // returning the cached copy to be tolerant. This preserves the
                    // previous behavior in restricted environments.
                    return new X509Certificate2(cached);
                }
            }
            else
            {
                // If the cache contains a negative result (null), it may be stale because
                // certificates can be re-added. Remove the negative cache and perform
                // a fresh lookup so newly added certificates are discovered.
                _cache.TryRemove(subjectName, out _);
            }
        }

        try
        {
            // Use a using block so the store is disposed quickly.
            using X509Store store = new(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);

                // Optimized lookup: use the built-in Find API instead of enumerating every certificate.
                X509Certificate2Collection found = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, validOnly: false);

                // If FriendlyName matching is required (friendly name is not part of FindBySubjectName),
                // check for explicit friendly name matches first.
                List<X509Certificate2> candidates = [];

                if (found != null && found.Count > 0)
                {
                    candidates.AddRange(found.Cast<X509Certificate2>());
                }

                // Additional friendly name matching for certificates that may not be found by subject name
                // (this is typically fast since we only iterate the small result set when Find returned nothing)
                if (candidates.Count == 0)
                {
                    // Fall back to enumerating but restrict to a quick projection to avoid expensive allocations.
                    foreach (X509Certificate2 c in store.Certificates)
                    {
                        if (!string.IsNullOrWhiteSpace(c.FriendlyName) && string.Equals(c.FriendlyName, subjectName, StringComparison.OrdinalIgnoreCase))
                        {
                            candidates.Add(c);
                        }
                    }
                }

                if (candidates.Count == 0)
                {
                    // Cache miss -> cache negative result to avoid repeated full scans
                    _cache[subjectName] = null;
                    return null;
                }

                // Prefer certificates with a private key and the most recent expiry date
                X509Certificate2? best = candidates
                    .OrderByDescending(c => c.NotAfter)
                    .FirstOrDefault(c => c.HasPrivateKey)
                    ?? candidates.OrderByDescending(c => c.NotAfter).FirstOrDefault();

                if (best != null)
                {
                    // Return a copy so the caller isn't tied to the store's lifetime
                    var result = new X509Certificate2(best);
                    // Cache the certificate instance for subsequent fast lookups
                    _cache[subjectName] = new X509Certificate2(best);
                    return result;
                }
            }
            finally
            {
                try { store.Close(); } catch { }
            }
        }
        catch
        {
            // Swallow exceptions intentionally: consumers of this service
            // expect a null result when a certificate cannot be accessed.
        }

        _cache[subjectName] = null;
        return null;
    }
}
