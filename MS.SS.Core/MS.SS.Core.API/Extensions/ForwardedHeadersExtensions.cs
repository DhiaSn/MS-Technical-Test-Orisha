using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace MS.SS.Core.API.Extensions;

public static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Trusts <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> when the API runs behind a reverse
    /// proxy, so the real client address reaches the rate limiter.
    /// </summary>
    /// <remarks>
    /// The proxies are listed from configuration rather than cleared: trusting the header from anyone
    /// would let a caller rotate it per request and walk through the sign-in throttle. With nothing
    /// configured only loopback is trusted, so a deployment that forgets to list its proxy loses the
    /// client address visibly instead of accepting a spoofable one.
    /// </remarks>
    public static IServiceCollection AddReverseProxyForwarding(
        this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue("BehindReverseProxy", true)) return services;

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            foreach (var proxy in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
            {
                if (IPAddress.TryParse(proxy, out var address)) options.KnownProxies.Add(address);
            }

            foreach (var network in configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
            {
                var parts = network.Split('/');

                if (parts.Length == 2
                    && IPAddress.TryParse(parts[0], out var prefix)
                    && int.TryParse(parts[1], out var prefixLength))
                {
                    options.KnownIPNetworks.Add(new System.Net.IPNetwork(prefix, prefixLength));
                }
            }
        });

        return services;
    }
}
