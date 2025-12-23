using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace Portfolio.Core.Abstracts;

/// <summary>
/// Provides business logic to create an application <see cref="ClaimsPrincipal"/>
/// from a client X509 certificate and to ensure any related persistence occurs.
/// </summary>
public interface ICertificateSignInService
{
    /// <summary>
    /// Creates a <see cref="ClaimsPrincipal"/> for the provided certificate.
    /// Implementations are expected to perform any necessary persistence (for
    /// example recording the certificate in the database) and return a populated
    /// principal or <c>null</c> when a principal cannot be created.
    /// </summary>
    /// <param name="cert">The client certificate.</param>
    /// <returns>A ClaimsPrincipal suitable for signing in, or null if no principal was created.</returns>
    Task<ClaimsPrincipal?> CreatePrincipalForCertificateAsync(X509Certificate2 cert);
}
