using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MS.SS.Core.Infrastructure.Database.Context;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c>, which builds the model without a host.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string LocalConnectionString =
        "Host=localhost;Port=5433;Database=reception;Username=reception;Password=reception_local_dev";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? LocalConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name))
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options, TimeProvider.System);
    }
}
