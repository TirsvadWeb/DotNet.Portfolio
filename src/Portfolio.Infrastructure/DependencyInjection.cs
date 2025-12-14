using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Portfolio.Core;
using Portfolio.Core.Abstracts;
using Portfolio.Infrastructure.Persistents;

namespace Portfolio.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCoreServices();

        // Register EF Core DbContext - use SQLite by default for local development
        string? conn = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(conn))
        {
            conn = "Data Source=portfolio.db";
        }

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(conn);
        });

        // Register repository for client certificates
        services.AddScoped<IClientCertificateRepository, Portfolio.Infrastructure.Repositories.ClientCertificateRepository>();
    }
}
