using Microsoft.EntityFrameworkCore;

using Portfolio.Core.Abstracts;
using Portfolio.Domain.Abstracts;
using Portfolio.Infrastructure.Persistents;

namespace Portfolio.Infrastructure.Repositories;

public class RepositoryBase<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    private readonly ApplicationDbContext _db;

    public RepositoryBase(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(TEntity entity)
    {
        await _db.Set<TEntity>().AddAsync(entity);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(TEntity entity)
    {
        _db.Set<TEntity>().Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _db.Set<TEntity>().ToListAsync();
    }

    public async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await _db.Set<TEntity>().FindAsync(id);
    }

    public async Task UpdateAsync(TEntity entity)
    {
        _db.Set<TEntity>().Update(entity);
        await _db.SaveChangesAsync();
    }
}
