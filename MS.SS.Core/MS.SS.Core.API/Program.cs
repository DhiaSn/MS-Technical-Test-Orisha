using MS.SS.Core.API.Extensions;
using MS.SS.Core.App.Extensions;
using MS.SS.Core.App.Seeding;
using MS.SS.Core.Infrastructure.Database.Extensions;
using MS.SS.Core.Security.Extensions;
using Serilog;

LogExtensions.AddBootstrapLog();

try
{
    Log.Information("Starting MS.SS.Core");

    var builder = WebApplication.CreateBuilder(args);

    builder.InitAppLog();

    builder.Services.AddReverseProxyForwarding(builder.Configuration);

    builder.AddCorsOrigins();

    builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

    builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);

    builder.Services.AddSecurityServices(builder.Configuration);

    builder.AddAppWolverine();

    builder.Services.AddApiContract();

    var app = builder.Build();

    await app.ApplyDatabaseMigrationsAsync();

    await app.SeedDemoDataAsync();

    app.UsePlatformPipeline();

    Log.Information("Application configured successfully; starting web host");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");

    // Not rethrown: the unhandled-exception dump would bury the Serilog entry. The exit code still
    // has to be non-zero, or Docker and CI read a failed boot as a clean shutdown.
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
