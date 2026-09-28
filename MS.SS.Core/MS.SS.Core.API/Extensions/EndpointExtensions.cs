using MS.SS.Core.API.Endpoints.Identity;

namespace MS.SS.Core.API.Extensions;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapAppEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthenticationEndpoints();

        return app;
    }
}
