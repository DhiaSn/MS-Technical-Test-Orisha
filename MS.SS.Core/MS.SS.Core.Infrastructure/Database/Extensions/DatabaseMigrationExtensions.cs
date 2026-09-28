using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MS.SS.Core.Infrastructure.Database.Context;

namespace MS.SS.Core.Infrastructure.Database.Extensions;

public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending migrations when <c>DatabaseSettings:AutoMigrate</c> is enabled.
    /// </summary>
    /// <remarks>
    /// A migration failure aborts startup rather than being logged and swallowed: continuing would
    /// leave the app serving requests against a schema it does not match, and the first symptom
    /// would be a write failing halfway through a request.
    /// </remarks>
    public static async Task<WebApplication> ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        var autoMigrate = app.Configuration.GetValue("DatabaseSettings:AutoMigrate", false);

        if (!autoMigrate)
        {
            app.Logger.LogInformation("Auto-migration disabled; skipping.");
            return app;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // The probe opens a connection outside the execution strategy unless wrapped, so without
        // this a database that is still starting would fail the boot instead of being retried.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        var pending = await strategy.ExecuteAsync(
            async () => (await dbContext.Database.GetPendingMigrationsAsync()).ToList());

        if (pending.Count == 0)
        {
            app.Logger.LogInformation("Database schema is up to date.");
            return app;
        }

        app.Logger.LogInformation(
            "Applying {Count} pending migration(s): {Migrations}", pending.Count, string.Join(", ", pending));

        await dbContext.Database.MigrateAsync();

        app.Logger.LogInformation("Migrations applied successfully.");

        return app;
    }
}
