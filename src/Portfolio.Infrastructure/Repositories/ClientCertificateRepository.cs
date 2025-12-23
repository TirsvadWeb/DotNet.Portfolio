using Microsoft.EntityFrameworkCore;

using Portfolio.Core.Abstracts;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Persistents;

namespace Portfolio.Infrastructure.Repositories;

/// <summary>
/// Repository for <see cref="ClientCertificate"/> entities.
/// <inheritdoc cref="IClientCertificateRepository" />
/// </summary>
/// <remarks>
/// Inheriting XML documentation with <c>&lt;inheritdoc/&gt;</c> copies the documentation
/// from the referenced base type or interface into this derived type or member.
/// This avoids duplication, keeps documentation consistent, and makes maintenance easier.
/// 
/// How to use:
/// - Place <c>&lt;inheritdoc/&gt;</c> on a class or member to inherit documentation from a base class
///   or an implemented interface. Optionally use the <c>cref</c> attribute to point to the
///   specific symbol to inherit from (for example <c>cref="IClientCertificateRepository"</c>).
/// 
/// Example:
/// <example>
/// <code>
/// // Resolve or instantiate the repository and call the method:
/// var repo = new ClientCertificateRepository(dbContext);
/// var cert = await repo.FindBySubjectAsync("CN=example");
/// </code>
/// </example>
/// </remarks>
public class ClientCertificateRepository(ApplicationDbContext db) : RepositoryBase<ClientCertificate>(db), IClientCertificateRepository
{
    /// <summary>
    /// Finds a <see cref="ClientCertificate"/> by subject.
    /// <inheritdoc cref="IClientCertificateRepository.FindBySubjectAsync(string)" />
    /// </summary>
    /// <param name="subject">The certificate subject to search for. Null or whitespace will return <c>null</c>.</param>
    /// <returns>The matching <see cref="ClientCertificate"/> or <c>null</c> if none is found.</returns>
    /// <remarks>
    /// The <c>&lt;inheritdoc/&gt;</c> above ensures this method's documentation is kept in sync
    /// with the interface definition. Additional implementation-specific details can be
    /// provided here without duplicating the contract documentation.
    /// </remarks>
    public async Task<ClientCertificate?> FindBySubjectAsync(string subject)
    {
        // Validate input early to avoid unnecessary database calls.
        if (string.IsNullOrWhiteSpace(subject)) return null;
        return await _db.ClientCertificates.FirstOrDefaultAsync(c => c.Subject == subject);
    }
}
