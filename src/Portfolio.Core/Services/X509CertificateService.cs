using Portfolio.Core.Abstracts.Services;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using static Portfolio.Core.Constants;

namespace Portfolio.Core.Services;

/// <summary>
/// Implementation of <see cref="IX509CertificateService"/> that retrieves X.509
/// certificates from the current user's certificate store.
/// </summary>
/// <remarks>
/// This class is an implementation of the <see cref="IX509CertificateService"/>
/// contract. Several members use the XML documentation tag `<inheritdoc/>` to
/// inherit documentation from that interface. Using `<inheritdoc/>` keeps
/// documentation consistent and DRY (Don't Repeat Yourself): update the
/// interface docs once and implementations automatically surface the same
/// guidance in generated API docs and IDE tooltips.
/// 
/// How to use `<inheritdoc/>`:
/// - Add `<inheritdoc/>` to a member's XML doc when that member implements an
///   interface member or overrides a base member that already has complete
///   documentation. The compiler and doc generators will copy the base/member
///   documentation into the implementation's docs.
/// - This is particularly useful for implementations that don't need different
///   behavioral documentation than the interface contract.
/// 
/// Why we have it:
/// - Reduces duplication across interface and implementation documentation.
/// - Ensures consumers see the authoritative contract documentation whether
///   they inspect the interface or concrete type.
/// 
/// <example>
/// Example usage (consumer code):
/// <code language="csharp"><![CDATA[
/// IX509CertificateService svc = new X509CertificateService();
/// // The documentation for GetCertificateByName is inherited from the
/// // IX509CertificateService interface via <inheritdoc/> on the implementing
/// // member in this class. Consumers will see the interface summary here.
/// var cert = svc.GetCertificateByName("MyCertSubjectOrFriendlyName");
/// if (cert != null)
/// {
///     Console.WriteLine($"Found cert thumbprint: {cert.Thumbprint}");
/// }
/// ]]></code>
/// </example>
/// </remarks>
public class X509CertificateService : IX509CertificateService
{
    // Predefined certificate subject/friendly name
    private const string PredefinedCertName = "TirsvadWebCert";

    // Small immutable DTO stored in the cache to minimize memory and allocations on the hot path.
    private sealed record CertificateCacheEntry(
        string Thumbprint,
        DateTime NotAfter,
        bool HasPrivateKey,
        byte[]? PfxExport);

    // Inline: cache uses ConcurrentDictionary with nullable values to represent
    // a positive cache entry (`CertificateCacheEntry`) or a negative cache (null).
    // Use a case-insensitive comparer so callers need not worry about casing.
    private readonly ConcurrentDictionary<string, CertificateCacheEntry?> _cache = new(StringComparer.OrdinalIgnoreCase);

    // Inline: guard dictionary used to prevent duplicate concurrent validations per subject.
    private readonly ConcurrentDictionary<string, byte> _validationRunning = new(StringComparer.OrdinalIgnoreCase);


    /// <inheritdoc/>
    public Task<X509Certificate2?> GetPreloadedCertificateAsync(string? predefinedCertName = null)
    {
        return GetCertificateByNameAsync(predefinedCertName ?? PredefinedCertName);
    }

    /// <inheritdoc/>
    public async Task<X509Certificate2?> GetCertificateByNameAsync(string subjectName)
    {
        if (string.IsNullOrWhiteSpace(subjectName))
        {
            return null;
        }

        // Check cache first
        if (_cache.TryGetValue(subjectName, out CertificateCacheEntry? cached))
        {
            Debug.WriteLine(string.Format(DEBUG_CACHE_HIT, subjectName, cached == null));
            if (cached != null)
            {
                // If we cached export bytes, validate presence in the store before reconstructing.
                if (cached.PfxExport != null)
                {
                    Debug.WriteLine(string.Format(DEBUG_CACHED_EXPORT_PRESENT, subjectName));
                    try
                    {
                        using X509Store validateStore = new(StoreName.My, StoreLocation.CurrentUser);
                        validateStore.Open(OpenFlags.ReadOnly);
                        X509Certificate2Collection found = validateStore.Certificates.Find(X509FindType.FindByThumbprint, cached.Thumbprint, validOnly: false);
                        Debug.WriteLine(string.Format(DEBUG_VALIDATE_STORE_FIND, found?.Count ?? 0, cached.Thumbprint));
                        if (found != null && found.Count > 0)
                        {
                            try
                            {
                                // Reconstruct from PFX export so callers receive an independent instance.
                                X509Certificate2 rs1 = X509CertificateLoader.LoadCertificate(cached.PfxExport!);
                                return rs1;
                            }
                            catch
                            {
                                // If reconstruction fails, remove the cache entry and fall through to fresh lookup
                                _cache.TryRemove(subjectName, out _);
                            }
                        }
                        else
                        {
                            _cache.TryRemove(subjectName, out _);
                        }
                    }
                    catch
                    {
                        // If store access fails, remove cache entry to avoid returning stale certs and continue to fresh lookup
                        _cache.TryRemove(subjectName, out _);
                    }
                }

                Debug.WriteLine(string.Format(DEBUG_CACHED_ENTRY_NO_EXPORT, subjectName));
                try
                {
                    using X509Store validateStore = new(StoreName.My, StoreLocation.CurrentUser);
                    validateStore.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection found = validateStore.Certificates.Find(X509FindType.FindByThumbprint, cached.Thumbprint, validOnly: false);
                    Debug.WriteLine(string.Format(DEBUG_VALIDATE_STORE_FIND, found?.Count ?? 0, cached.Thumbprint));
                    if (found != null && found.Count > 0)
                    {
                        return new X509Certificate2(found[0]);
                    }
                    else
                    {
                        _cache.TryRemove(subjectName, out _);
                    }
                }
                catch
                {
                    // If store access fails and we couldn't reconstruct earlier, remove cache entry and continue.
                    _cache.TryRemove(subjectName, out _);
                }
            }
            else
            {
                _cache.TryRemove(subjectName, out _);
            }
        }

        try
        {
            // Offload blocking store operations to a background thread pool thread to avoid blocking callers.
            return await Task.Run(() =>
            {
                using X509Store store = new(StoreName.My, StoreLocation.CurrentUser);
                try
                {
                    store.Open(OpenFlags.ReadOnly);

                    // Optimized lookup: use the built-in Find API instead of enumerating every certificate.
                    X509Certificate2Collection found = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, validOnly: false);
                    Debug.WriteLine(string.Format(DEBUG_FRESH_LOOKUP, subjectName, found?.Count ?? 0));

                    List<X509Certificate2> candidates = [];

                    if (found != null && found.Count > 0)
                    {
                        candidates.AddRange(found.Cast<X509Certificate2>());
                    }

                    // Additional friendly name matching for certificates that may not be found by subject name
                    // (this is typically fast since we only iterate the small result set when Find returned nothing)
                    if (candidates.Count == 0)
                    {
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
                        Debug.WriteLine(string.Format(DEBUG_NO_CANDIDATES, subjectName));
                        return null;
                    }

                    // Prefer certificates with a private key and the most recent expiry date
                    X509Certificate2? best = candidates
                        .OrderByDescending(c => c.NotAfter)
                        .FirstOrDefault(c => c.HasPrivateKey)
                        ?? candidates.OrderByDescending(c => c.NotAfter).FirstOrDefault();

                    if (best != null)
                    {
                        // Attempt to export a PFX payload for quick reconstruction on the hot path.
                        byte[]? export = null;
                        try
                        {
                            // Export may fail for non-exportable keys; swallow errors and continue without export.
                            export = best.Export(X509ContentType.Pfx, string.Empty);
                            Debug.WriteLine(string.Format(DEBUG_EXPORT_SUCCEEDED, subjectName, export?.Length ?? 0));
                        }
                        catch { Debug.WriteLine(string.Format(DEBUG_EXPORT_FAILED, subjectName)); }

                        // Cache a small immutable DTO with optional exported bytes.
                        CertificateCacheEntry entry = new(best.Thumbprint, best.NotAfter, best.HasPrivateKey, export);
                        _cache[subjectName] = entry;
                        Debug.WriteLine(string.Format(DEBUG_CACHED_ENTRY_CACHED, subjectName, (export != null)));

                        // NOTE: reconstructing from export so returned instance is independent of the store
                        if (export != null)
                        {
                            try
                            {
                                X509Certificate2 reconstructed = X509CertificateLoader.LoadCertificate(export);
                                return reconstructed;
                            }
                            catch
                            {
                                Debug.WriteLine(string.Format(DEBUG_RECONSTRUCTION_FAILED, subjectName));
                            }
                        }

                        // Return a copy of the store-backed certificate so caller does not get the store-owned instance
                        return new X509Certificate2(best);
                    }

                    return null;
                }
                finally
                {
                    try { store.Close(); } catch { }
                }
            });
        }
        catch
        {
            // Swallow exceptions intentionally: consumers of this service
            // expect a null result when a certificate cannot be accessed.
        }

        _cache[subjectName] = null;
        return null;
    }

    public async Task<X509Certificate2?> CreateCertificateAsync(string subjectName)
    {
        // If running in Development, attempt to create a self-signed certificate and add to CurrentUser\My
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        if (string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using RSA rsa = RSA.Create(2048);
                X500DistinguishedName dn = new($"CN={subjectName}");
                CertificateRequest req = new(dn, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
                req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.2")], false)); // Client Authentication

                DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddDays(-1);
                DateTimeOffset notAfter = DateTimeOffset.UtcNow.AddYears(20);

                using X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter);

                // Ensure certificate contains exportable private key by creating a PFX and re-importing.
                X509Certificate2 certWithKey;
                try
                {
                    byte[] pfx = cert.Export(X509ContentType.Pfx, string.Empty);
                    //certWithKey = new X509Certificate2(pfx, string.Empty, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                    certWithKey = X509CertificateLoader.LoadPkcs12(pfx, string.Empty, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                }
                catch
                {
                    certWithKey = X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), string.Empty);
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    certWithKey.FriendlyName = subjectName;
                }

                try
                {
                    using X509Store writeStore = new(StoreName.My, StoreLocation.CurrentUser);
                    writeStore.Open(OpenFlags.ReadWrite);
                    writeStore.Add(certWithKey);
                    writeStore.Close();
                }
                catch
                {
                    // ignore store add errors
                }

                // Cache and return
                byte[]? export = null;
                try { export = certWithKey.Export(X509ContentType.Pfx, string.Empty); } catch { }
                CertificateCacheEntry entry = new(certWithKey.Thumbprint, certWithKey.NotAfter, certWithKey.HasPrivateKey, export);
                _cache[subjectName] = entry;
                if (export != null)
                {
                    try { return X509CertificateLoader.LoadCertificate(export); } catch { }
                }

                return new X509Certificate2(certWithKey);
            }
            catch
            {
                // ignore creation errors and fall through to negative cache
            }
        }
        return null;
    }
}
