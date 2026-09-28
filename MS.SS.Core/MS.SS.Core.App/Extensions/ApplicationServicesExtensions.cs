using MS.SS.Core.App.Seeding;
using MS.SS.Core.Modules.Identity.Infrastructure.Extensions;

namespace MS.SS.Core.App.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddIdentityModule();
        services.AddAppRateLimiter(configuration);
        services.AddScoped<DemoDataSeeder>();

        return services;
    }
}
