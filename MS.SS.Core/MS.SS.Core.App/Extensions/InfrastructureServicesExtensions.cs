using MS.SS.Core.Infrastructure.Database.Extensions;

namespace MS.SS.Core.App.Extensions;

public static class InfrastructureServicesExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddDatabaseServices(configuration, environment);
        services.AddPlatformHealthChecks();

        return services;
    }
}
