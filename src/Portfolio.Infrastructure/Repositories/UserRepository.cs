using Portfolio.Core.Abstracts.Repositories;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Persistents;

namespace Portfolio.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for <see cref="ApplicationUser"/> domain entities.
/// <para>
/// This concrete implementation fulfills the <see cref="IUserRepository"/> contract. Documentation
/// for the public API is inherited from the interface using <c>&lt;inheritdoc/&gt;</c> so that
/// the interface remains the single source of truth for behavior and remarks.
/// </para>
/// </summary>
/// <remarks>
/// How to use and why we have it:
/// - Register the implementation with dependency injection and depend on <see cref="IUserRepository"/> in consumers.
/// - Using <c>&lt;inheritdoc/&gt;</c> keeps documentation DRY: changes to the interface docs automatically apply here.
/// </remarks>
/// <example>
/// Example: register and consume the repository via DI
/// <code>
/// // Program.cs
/// services.AddScoped&lt;IUserRepository, UserRepository&gt;();
///
/// // Usage in a service or Blazor component
/// public class UserService
/// {
///     private readonly IUserRepository _users;
///     public UserService(IUserRepository users) => _users = users;
///     public Task&lt;ApplicationUser?&gt; GetAsync(Guid id) => _users.FindAsync(id);
/// }
/// </code>
/// </example>
/// <inheritdoc cref="IUserRepository"/>
public class UserRepository(ApplicationDbContext db) : RepositoryBase<ApplicationUser>(db), IUserRepository
{
    // Inline comment: This implementation relies on RepositoryBase for CRUD behavior and intentionally exposes no extra members here.
}
