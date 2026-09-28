using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MS.SS.Core.App.Http;
using MS.SS.Core.SharedKernel.Common;

namespace MS.SS.Core.App.Extensions;

public static class RateLimiterExtensions
{
    /// <summary>Applied to sign-in and register, the two endpoints an attacker can brute-force.</summary>
    public const string AuthPolicy = "MsAuthPolicy";

    private const int DefaultPermitsPerMinute = 10;
    private const int FallbackRetryAfterSeconds = 60;

    public static IServiceCollection AddAppRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        var permits = configuration.GetValue("RateLimiting:AuthPermitsPerMinute", DefaultPermitsPerMinute);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;

            // RemoteIpAddress is the forwarded client address once the forwarded-headers middleware
            // has run, so users behind the web container do not share one budget.
            options.AddPolicy(AuthPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = permits,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });

        return services;
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait)
            ? Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds))
            : FallbackRetryAfterSeconds;

        context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);

        await ProblemResponses.WriteAsync(
            context.HttpContext,
            StatusCodes.Status429TooManyRequests,
            ErrorCodes.RateLimited,
            "Too many requests. Please try again later.",
            new Dictionary<string, object?> { ["retryAfterSeconds"] = retryAfter },
            cancellationToken);
    }
}
