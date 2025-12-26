using Microsoft.EntityFrameworkCore;

using Portfolio.Infrastructure.Persistents;

namespace Portfolio.Infrastructure.Services;

public interface IDbContextOptionsServices
{
    DbContextOptions<ApplicationDbContext> CreateOptions(string environment, string? name = null);
    string CreateConnectionString(string environment, string? name = null);
}