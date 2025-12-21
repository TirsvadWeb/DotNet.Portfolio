using Portfolio.Core.Abstracts;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

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
    public Task<X509Certificate2?> GetPreloadedCertificateAsync()
    {
        return GetCertificateByNameAsync(PredefinedCertName);
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
            Debug.WriteLine($"GetCertificateByName: cache hit for '{subjectName}'. Cached is null: {cached == null}");
            if (cached != null)
            {
                // If we cached export bytes, validate presence in the store before reconstructing.
                if (cached.PfxExport != null)
                {
                    Debug.WriteLine($"GetCertificateByName: cached export present for '{subjectName}', validating presence in store before returning reconstructed cert.");
                    try
                    {
                        using X509Store validateStore = new(StoreName.My, StoreLocation.CurrentUser);
                        validateStore.Open(OpenFlags.ReadOnly);
                        X509Certificate2Collection found = validateStore.Certificates.Find(X509FindType.FindByThumbprint, cached.Thumbprint, validOnly: false);
                        Debug.WriteLine($"GetCertificateByName: validate store find returned {(found?.Count ?? 0)} results for thumb {cached.Thumbprint}.");
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

                Debug.WriteLine($"GetCertificateByName: cached entry present for '{subjectName}' but no cached export - validating in store.");
                try
                {
                    using X509Store validateStore = new(StoreName.My, StoreLocation.CurrentUser);
                    validateStore.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection found = validateStore.Certificates.Find(X509FindType.FindByThumbprint, cached.Thumbprint, validOnly: false);
                    Debug.WriteLine($"GetCertificateByName: validate store find returned {(found?.Count ?? 0)} results for thumb {cached.Thumbprint}.");
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
                    Debug.WriteLine($"GetCertificateByName: fresh lookup by subject '{subjectName}' returned {(found?.Count ?? 0)} results.");

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
                        Debug.WriteLine($"GetCertificateByName: no candidates found for '{subjectName}' - caching negative result.");
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
                            Debug.WriteLine($"GetCertificateByName: export succeeded for '{subjectName}', size={export?.Length ?? 0}.");
                        }
                        catch { Debug.WriteLine($"GetCertificateByName: export failed for '{subjectName}'."); }

                        // Cache a small immutable DTO with optional exported bytes.
                        CertificateCacheEntry entry = new(best.Thumbprint, best.NotAfter, best.HasPrivateKey, export);
                        _cache[subjectName] = entry;
                        Debug.WriteLine($"GetCertificateByName: cached entry for '{subjectName}' (HasExport={(export != null)}).");

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
                                Debug.WriteLine($"GetCertificateByName: reconstruction from export failed for '{subjectName}', falling back to copy of store certificate.");
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
}
