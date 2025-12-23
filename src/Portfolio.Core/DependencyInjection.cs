using Microsoft.Extensions.DependencyInjection;

using Portfolio.Core.Abstracts.Services;
using Portfolio.Core.Services;

namespace Portfolio.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddScoped<IX509CertificateService, X509CertificateService>();
        return services;
    }
}
