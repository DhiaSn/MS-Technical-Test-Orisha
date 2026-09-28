using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MS.SS.Core.Infrastructure.Database.Context;
using MS.SS.Core.Infrastructure.Database.Repositories;
using MS.SS.Core.SharedKernel.Interfaces;

namespace MS.SS.Core.Infrastructure.Database.Extensions;

public static class DatabaseServiceExtensions
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = GetConnectionString(configuration);

        AddAppDbContext(services, connectionString, environment);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured. " +
                "Set it via user-secrets or the ConnectionStrings__DefaultConnection environment variable.");
        }

        return connectionString;
    }

    private static void AddAppDbContext(IServiceCollection services, string connectionString, IHostEnvironment environment)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            ConfigureNpgsql(options, connectionString);

            if (environment.IsDevelopment()) EnableDevelopmentDiagnostics(options);
        }, ServiceLifetime.Scoped);
    }

    private static void ConfigureNpgsql(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            })
            .UseSnakeCaseNamingConvention();

    private static void EnableDevelopmentDiagnostics(DbContextOptionsBuilder options)
    {
        options.EnableDetailedErrors();
    }
}
