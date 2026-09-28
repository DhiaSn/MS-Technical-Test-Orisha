using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.Security.Models;
using MS.SS.Core.Security.Models.Tokens;
using MS.SS.Core.Security.Services;

namespace MS.SS.Core.Security.Extensions;

public static class SecurityServiceExtensions
{
    private const string TokenOptionsSection = "TokenOptions";

    public static IServiceCollection AddSecurityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var tokenOptions = services.AddTokenOptions(configuration);
        var signingConfigurations = services.AddSigningConfigurations(tokenOptions);

        services.AddJwtBearerAuthentication(tokenOptions, signingConfigurations);
        services.AddSecurityAuthorization();
        services.AddSecurityServiceImplementations();

        return services;
    }

    private static TokenOptions AddTokenOptions(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(TokenOptionsSection);
        services.Configure<TokenOptions>(section);

        return section.Get<TokenOptions>() ?? new TokenOptions();
    }

    private static SigningConfigurations AddSigningConfigurations(
        this IServiceCollection services,
        TokenOptions tokenOptions)
    {
        // Constructing this at startup is deliberate: a missing or too-short signing secret fails
        // the boot rather than the first sign-in attempt in production.
        var signingConfigurations = new SigningConfigurations(tokenOptions.Secret);
        services.AddSingleton(signingConfigurations);

        return signingConfigurations;
    }

    private static IServiceCollection AddJwtBearerAuthentication(
        this IServiceCollection services,
        TokenOptions tokenOptions,
        SigningConfigurations signingConfigurations)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = tokenOptions.Issuer,
                    ValidAudience = tokenOptions.Audience,
                    IssuerSigningKey = signingConfigurations.SecurityKey,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    // The SPA holds the token in an HttpOnly cookie, so it never reaches JavaScript.
                    // An Authorization header still wins when present, which keeps Scalar and
                    // integration tests working without cookie plumbing.
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token)
                            && string.IsNullOrEmpty(context.Request.Headers.Authorization)
                            && context.Request.Cookies.TryGetValue(SecurityConstants.AccessTokenCookie, out var cookieToken)
                            && !string.IsNullOrEmpty(cookieToken))
                        {
                            context.Token = cookieToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    private static IServiceCollection AddSecurityAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Endpoints are authenticated unless they opt out with AllowAnonymous. Forgetting
            // RequireAuthorization on a new endpoint group then fails closed, not open.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    private static IServiceCollection AddSecurityServiceImplementations(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITokenService, TokenService>();

        return services;
    }
}
