using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MS.SS.Core.Infrastructure.Database.Context;

namespace MS.SS.Core.Infrastructure.Database.Extensions;

public static class DatabaseHealthCheckExtensions
{
    private const string DatabaseCheckName = "database";

    // Shorter than a probe's own timeout: with the connection retry policy an unreachable
    // database would otherwise keep the check pending for tens of seconds.
    private static readonly TimeSpan DatabaseCheckTimeout = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddPlatformHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("api", () => HealthCheckResult.Healthy("API is running"), tags: ["api", "live"])
            .AddDbContextCheck<AppDbContext>(
                name: DatabaseCheckName,
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "postgresql", "ready"]);

        services.Configure<HealthCheckServiceOptions>(options =>
            options.Registrations.Single(r => r.Name == DatabaseCheckName).Timeout = DatabaseCheckTimeout);

        return services;
    }
}
