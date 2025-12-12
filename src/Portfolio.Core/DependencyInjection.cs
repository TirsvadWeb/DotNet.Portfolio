using Microsoft.Extensions.DependencyInjection;

namespace Portfolio.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IX509CertificateService, X509CertificateService>();
        return services;
    }
}
