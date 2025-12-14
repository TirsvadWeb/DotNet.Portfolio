using System.Security.Cryptography.X509Certificates;

namespace Portfolio.Core.Abstracts;

// Inline comment: Prefer using <inheritdoc/> on implementing members so they inherit the interface documentation.
/// <summary>
/// Service for retrieving X.509 certificates used by the application.
/// </summary>
/// <remarks>
/// This interface defines methods to obtain certificates from preloaded
/// configuration or by subject name. Implementations should inherit XML
/// documentation using <c>&lt;inheritdoc/&gt;</c> so that the documentation
/// stays close to the interface contract and consumers see consistent comments.
/// 
/// Why use <c>&lt;inheritdoc/&gt;</c>:
/// - Keeps the documentation DRY: method details are declared once on the
///   interface and reused by implementations.
/// - Ensures consistency between the contract and the implementation.
/// - Tooling (IDE and doc generators) will merge the comments so consumers
///   benefit from the interface-level documentation.
/// </remarks>
/// <example>
/// Example implementation showing how to inherit documentation:
/// <code>
/// public class X509CertificateService : IX509CertificateService
/// {
///     /// <inheritdoc/>
///     public X509Certificate2? GetPreloadedCertificate()
///     {
///         // return the preloaded certificate or null
///     }
///
///     /// <inheritdoc/>
///     public X509Certificate2? GetCertificateByName(string subjectName)
///     {
///         // lookup and return certificate by subject name
///     }
/// }
/// </code>
/// </example>
public interface IX509CertificateService
{
    /// <summary>
    /// Returns the predefined certificate if found, otherwise <c>null</c>.
    /// </summary>
    /// <returns>The predefined certificate or <c>null</c>.</returns>
    /// <remarks>
    /// Implementations may use <c>&lt;inheritdoc/&gt;</c> to inherit this
    /// documentation on the concrete member.
    /// </remarks>
    X509Certificate2? GetPreloadedCertificate();

    /// <summary>
    /// Attempts to find a certificate by subject name and returns it or <c>null</c>.
    /// </summary>
    /// <param name="subjectName">The subject name of the certificate to find.</param>
    /// <returns>The found certificate or <c>null</c>.</returns>
    /// <remarks>
    /// Use <c>&lt;inheritdoc/&gt;</c> on the implementing method to inherit this
    /// description and keep documentation consistent.
    /// </remarks>
    X509Certificate2? GetCertificateByName(string subjectName);

}
