using MS.SS.Core.API.Endpoints.Identity;
using MS.SS.Core.API.Endpoints.Reception;

namespace MS.SS.Core.API.Extensions;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapAppEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthenticationEndpoints();
        app.MapDeliveryEndpoints();

        return app;
    }
}
