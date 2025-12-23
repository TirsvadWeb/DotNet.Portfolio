using Portfolio.Core.Abstracts;
using Portfolio.Domain.Entities;

using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Concurrent;
using System.Threading;

namespace Portfolio.Infrastructure.Services;

/// <summary>
/// <inheritdoc />
/// </summary>
/// <remarks>
/// This implementation inherits its contract documentation from <see cref="ICertificateSignInService" /> using
/// <c>&lt;inheritdoc/&gt;</c>. Inheriting documentation reduces duplication and keeps comments consistent with the
/// interface when the contract changes.
/// 
/// How to use:
/// - Register the service with dependency injection (e.g. <c>builder.Services.AddScoped&lt;ICertificateSignInService, CertificateSignInService&gt;()</c>).
/// - Call <see cref="CreatePrincipalForCertificateAsync"/> with an <see cref="X509Certificate2"/> to obtain a
///   <see cref="ClaimsPrincipal"/> representing the certificate subject.
/// 
/// Why we have it:
/// - Provides a central place to transform a client certificate into an application principal and to persist
///   certificate metadata when first seen.
/// - Using <c>&lt;inheritdoc/&gt;</c> keeps interface and implementation documentation synchronized.
/// </remarks>
/// <example>
/// Example usage:
/// <code language="csharp">
/// // Register in DI (Program.cs)
/// builder.Services.AddScoped&lt;ICertificateSignInService, CertificateSignInService&gt;();
///
/// // Resolve and use
/// var certService = serviceProvider.GetRequiredService&lt;ICertificateSignInService&gt;();
/// var cert = new X509Certificate2("path/to/cert.pfx", "password");
/// var principal = await certService.CreatePrincipalForCertificateAsync(cert);
/// if (principal != null)
/// {
///     // Sign in or use the principal
/// }
/// </code>
/// </example>
public class CertificateSignInService(IClientCertificateRepository repo) : ICertificateSignInService
{
    private readonly IClientCertificateRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    private const string CookieScheme = "Cookies";

    // Per-subject locks to avoid multiple concurrent adds when callers race to create the same entity.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _subjectLocks = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// <inheritdoc cref="ICertificateSignInService.CreatePrincipalForCertificateAsync" />
    /// </summary>
    /// <param name="cert">The client certificate to create a principal for.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> when the certificate could be converted; otherwise <c>null</c>.</returns>
    public async Task<ClaimsPrincipal?> CreatePrincipalForCertificateAsync(X509Certificate2 cert)
    {
        if (cert == null) return null;

        try
        {
            string subject = cert.Subject ?? string.Empty;

            // Use a per-subject semaphore to ensure only one caller will attempt to add the certificate when missing.
            var sem = _subjectLocks.GetOrAdd(subject, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync();
            try
            {
                ClientCertificate? existing = await _repo.FindBySubjectAsync(subject);
                if (existing == null)
                {
                    ClientCertificate entity = new()
                    {
                        Id = Guid.NewGuid(),
                        Subject = subject,
                        Issuer = cert.Issuer ?? string.Empty,
                        SerialNumber = cert.SerialNumber ?? string.Empty,
                        ValidFrom = cert.NotBefore,
                        ValidTo = cert.NotAfter
                    };

                    await _repo.AddAsync(entity);
                }
            }
            finally
            {
                sem.Release();
            }

            // Build claims from certificate properties
            List<Claim> claims =
            [
                new Claim(ClaimTypes.Name, cert.Subject ?? string.Empty),
                new Claim("thumbprint", cert.Thumbprint ?? string.Empty),
                new Claim("issuer", cert.Issuer ?? string.Empty),
                new Claim("serialNumber", cert.SerialNumber ?? string.Empty)
            ];

            ClaimsIdentity identity = new(claims, CookieScheme);
            ClaimsPrincipal principal = new(identity);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
