using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Portfolio.Core;
using Portfolio.Core.Abstracts;
using Portfolio.Infrastructure.Persistents;
using Portfolio.Infrastructure.Services;

namespace Portfolio.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCoreServices();

        // Determine environment (Development, Test, Production/Release)
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                      ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                      ?? configuration["Environment"];

        // Read user-secrets id from configuration (appsettings.json). Keys checked in order:
        // "Portfolio:UserSecretsId", "UserSecrets:Id", "UserSecretsId". Falls back to the known id if not present.
        string? userSecretsId = configuration["Portfolio:UserSecretsId"]
                                ?? configuration["UserSecrets:Id"]
                                ?? configuration["UserSecretsId"]
                                ?? "0cf4f171-3a18-4cbc-a691-09a51dbb2c5e";

        // Register DbContext options helper using configured user-secrets id so it can read secrets.json
        services.AddScoped<IDbContextOptionsServices>(_ => new DbContextOptionsServices(userSecretsId));

        // Use the helper to build connection string that respects user-secrets and environment variables (e.g. DB_HOST from docker-compose)
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var optsSvc = sp.GetRequiredService<IDbContextOptionsServices>();
            string conn = optsSvc.CreateConnectionString(env ?? string.Empty);
            options.UseSqlServer(conn, sqlOptions => sqlOptions.EnableRetryOnFailure());
        });

        // Register repository for client certificates
        services.AddScoped<IClientCertificateRepository, Portfolio.Infrastructure.Repositories.ClientCertificateRepository>();

        // Register certificate sign-in service implementation (implementation lives in Infrastructure, interface is in Core.Abstracts)
        services.AddScoped<ICertificateSignInService, CertificateSignInService>();
    }
}
