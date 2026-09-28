namespace MS.SS.Core.App.Seeding;

public static class SeedExtensions
{
    public static async Task<WebApplication> SeedDemoDataAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Seeding:Enabled", false))
        {
            app.Logger.LogInformation("Demo data seeding disabled; skipping.");
            return app;
        }

        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(app.Lifetime.ApplicationStopping);

        return app;
    }
}
