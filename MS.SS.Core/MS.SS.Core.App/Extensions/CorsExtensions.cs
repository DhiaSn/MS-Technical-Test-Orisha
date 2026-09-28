namespace MS.SS.Core.App.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "ReceptionAllowedOrigins";

    public static void AddCorsOrigins(this WebApplicationBuilder builder)
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

        // No AllowCredentials: the browser reaches the API through its own origin, so no cookie
        // ever crosses origins and the wildcard-with-credentials trap never comes into play.
        builder.Services.AddCors(options =>
            options.AddPolicy(PolicyName, policy => policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()));
    }

    public static void UseAppCors(this WebApplication app) => app.UseCors(PolicyName);
}
