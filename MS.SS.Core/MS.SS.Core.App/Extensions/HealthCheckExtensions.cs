using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace MS.SS.Core.App.Extensions;

public static class HealthCheckExtensions
{
    public static void MapAppHealthChecks(this WebApplication app)
    {
        // Liveness must not touch the database, or a database blip would make the orchestrator
        // restart a healthy process.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthResponse
        }).AllowAnonymous();

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthResponse
        }).AllowAnonymous();
    }

    private static Task WriteHealthResponse(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                // The exception message is omitted on purpose: the endpoint is anonymous and a
                // failure description can carry a connection-string fragment.
                duration = e.Value.Duration
            })
        });

        return context.Response.WriteAsync(payload);
    }
}
