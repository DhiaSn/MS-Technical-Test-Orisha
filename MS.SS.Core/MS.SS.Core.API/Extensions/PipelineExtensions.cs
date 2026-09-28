using MS.SS.Core.API.Middlewares;
using MS.SS.Core.App.Extensions;
using Scalar.AspNetCore;

namespace MS.SS.Core.API.Extensions;

public static class PipelineExtensions
{
    public static void UsePlatformPipeline(this WebApplication app)
    {
        // First, so every later component sees the real client address and scheme.
        app.UseForwardedHeaders();

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseStatusCodePages();

        if (!app.Environment.IsProduction() && !app.Environment.IsStaging())
        {
            app.MapOpenApi().AllowAnonymous();
            app.MapScalarApiReference().AllowAnonymous();
        }

        // After the exception middleware so an error response still carries the CORS headers the
        // browser needs in order to read it.
        app.UseAppCors();

        app.UseMiddleware<CsrfProtectionMiddleware>();

        // TLS is terminated by the proxy in front of the API, so redirection is opt-out.
        if (!app.Configuration.GetValue("BehindReverseProxy", true))
        {
            app.UseHttpsRedirection();
        }

        app.UseRouting();

        app.UseAuthentication();

        // After routing so the per-endpoint policy resolves, before authorization so a flood of
        // sign-in attempts is refused without being evaluated.
        app.UseRateLimiter();

        app.UseAuthorization();

        app.MapAppHealthChecks();

        app.MapAppEndpoints();
    }
}
