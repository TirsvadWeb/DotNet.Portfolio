using Microsoft.EntityFrameworkCore;

using Portfolio.Core.Abstracts.Repositories;
using Portfolio.Domain.Abstracts;
using Portfolio.Infrastructure.Persistents;

namespace Portfolio.Infrastructure.Repositories;

/// <summary>
/// Base repository providing common CRUD operations for entities.
/// </summary>
/// <remarks>
/// This class implements <see cref="IRepository{TEntity}"/>. Public member documentation is inherited
/// from the interface using the <c>&lt;inheritdoc/&gt;</c> tag so consumers see the contract's XML documentation
/// without duplicating it here.
/// 
/// How to use and why we have it:
/// - Use this base repository to avoid repeating common data-access code (Add, Delete, Update, GetAll, GetById).
/// - The <c>&lt;inheritdoc/&gt;</c> tag tells the compiler to copy documentation from the interface, keeping
///   documentation centralized and consistent.
/// 
/// Code example:
/// <code>
/// // Assume `db` is an instance of ApplicationDbContext and `ApplicationUser` implements IEntity
/// var userRepository = new RepositoryBase<ApplicationUser>(db);
/// await userRepository.AddAsync(new ApplicationUser { /* ... */ });
/// var allUsers = await userRepository.GetAllAsync();
/// </code>
/// </remarks>
public class RepositoryBase<TEntity>(ApplicationDbContext db) : IRepository<TEntity> where TEntity : class, IEntity
{
    // Application database context injected via primary constructor.
    protected readonly ApplicationDbContext _db = db; // inline comment: stores the EF Core DbContext

    /// <inheritdoc/>
    public async Task AddAsync(TEntity entity)
    {
        await _db.Set<TEntity>().AddAsync(entity);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(TEntity entity)
    {
        _db.Set<TEntity>().Remove(entity);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _db.Set<TEntity>().ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await _db.Set<TEntity>().FindAsync(id);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(TEntity entity)
    {
        _db.Set<TEntity>().Update(entity);
        await _db.SaveChangesAsync();
    }
}
