using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MS.SS.Core.Tests.TestHelpers;

public sealed class ReceptionApiFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", postgres.ConnectionString);
        builder.UseSetting("DatabaseSettings:AutoMigrate", "true");
        builder.UseSetting("Seeding:Enabled", "true");
    }
}
