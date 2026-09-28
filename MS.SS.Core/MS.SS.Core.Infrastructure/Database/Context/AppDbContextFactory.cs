using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MS.SS.Core.Infrastructure.Database.Context;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c>, which builds the model without a host.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var apiProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "MS.SS.Core.API");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(apiProjectPath) ? apiProjectPath : Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets("ms-ss-core-api")
            .AddEnvironmentVariables()
            .Build();

        // Scaffolding only reads the model, never the database, so a placeholder is fine when no
        // connection string is configured — `dotnet ef migrations add` must work on a fresh clone.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=reception_design_time;Username=reception;Password=reception_local_dev";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name))
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options, TimeProvider.System);
    }
}
