using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MS.SS.Core.Modules.Reception.Application.Interfaces;
using MS.SS.Core.Modules.Reception.Domain.Entities;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.Security.Models;
using MS.SS.Core.SharedKernel.Interfaces;

namespace MS.SS.Core.Tests.TestHelpers;

public sealed class ReceptionApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    /// <param name="postgres">The shared container.</param>
    /// <param name="connectionString">An existing database to boot against; by default the factory gets one of its own.</param>
    public ReceptionApiFactory(PostgresFixture postgres, string? connectionString = null) =>
        _connectionString = connectionString ?? postgres.CreateDatabase();

    public int AuthPermitsPerMinute { get; init; } = 1_000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("DatabaseSettings:AutoMigrate", "true");
        builder.UseSetting("Seeding:Enabled", "true");
        builder.UseSetting("TokenOptions:Secret", new string('k', 48));
        builder.UseSetting("TokenOptions:Issuer", "ms.ss.core");
        builder.UseSetting("TokenOptions:Audience", "ms.ca.clientapp");
        builder.UseSetting("RateLimiting:AuthPermitsPerMinute", AuthPermitsPerMinute.ToString());
        builder.UseSetting("AllowedOrigins:0", "http://localhost:3000");
    }

    public async Task SeedAsync(Delivery delivery)
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDeliveryRepository>().Add(delivery);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }

    /// <summary>A client signed in as a fresh operator through a minted Bearer token, without any password hashing.</summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var token = Services.GetRequiredService<ITokenService>()
            .CreateAccessToken(new TokenPrincipal(Guid.NewGuid(), IdentityRoles.Operator));
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token.Token);

        return client;
    }
}
