using Microsoft.Extensions.DependencyInjection;

using Portfolio.Core;

namespace Portfolio.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddCoreServices();
    }
}
