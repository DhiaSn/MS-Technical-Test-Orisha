using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.Security.Models;

namespace MS.SS.Core.Tests.TestHelpers;

public sealed class ReceptionApiFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    public int AuthPermitsPerMinute { get; init; } = 1_000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", postgres.ConnectionString);
        builder.UseSetting("DatabaseSettings:AutoMigrate", "true");
        builder.UseSetting("Seeding:Enabled", "true");
        builder.UseSetting("TokenOptions:Secret", new string('k', 48));
        builder.UseSetting("TokenOptions:Issuer", "ms.ss.core");
        builder.UseSetting("TokenOptions:Audience", "ms.ca.clientapp");
        builder.UseSetting("RateLimiting:AuthPermitsPerMinute", AuthPermitsPerMinute.ToString());
        builder.UseSetting("AllowedOrigins:0", "http://localhost:3000");
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
