using System.Security.Cryptography.X509Certificates;

namespace Portfolio.Core.Abstracts.Services;

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
    Task<X509Certificate2?> CreateCertificateAsync(string subjectName);

    /// <summary>
    /// Asynchronously attempts to find a certificate by subject name and returns it or <c>null</c>.
    /// </summary>
    /// <param name="subjectName">The subject name of the certificate to find.</param>
    /// <returns>A task that completes with the found certificate or <c>null</c>.</returns>
    /// <remarks>
    /// This asynchronous variant performs the same work as <see cref="GetCertificateByName"/>
    /// but returns a <see cref="Task{TResult}"/> so callers can avoid blocking the calling thread.
    /// Implementations may offload blocking store I/O to a background thread.
    /// </remarks>
    Task<X509Certificate2?> GetCertificateByNameAsync(string subjectName);

    /// <summary>
    /// Asynchronously returns the predefined certificate if found, otherwise <c>null</c>.
    /// </summary>
    /// <param name="predefinedCertName">
    /// The name of the predefined certificate to retrieve.
    /// If <c>null</c>, a default predefined certificate is returned.
    /// </param>
    /// <returns>A task that completes with the predefined certificate or <c>null</c>.</returns>
    /// <remarks>
    /// Prefer callers use this asynchronous variant to avoid blocking threads when the
    /// underlying certificate store I/O may be slow.
    /// </remarks>
    Task<X509Certificate2?> GetPreloadedCertificateAsync(string? predefinedCertName = null);
}
