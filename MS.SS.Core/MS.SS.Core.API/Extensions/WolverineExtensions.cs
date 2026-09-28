using MS.SS.Core.Modules.Identity.Infrastructure.Extensions;
using MS.SS.Core.Modules.Reception.Infrastructure.Extensions;
using Wolverine;

namespace MS.SS.Core.API.Extensions;

public static class WolverineExtensions
{
    public static void AddAppWolverine(this WebApplicationBuilder builder)
    {
        builder.Host.UseWolverine(opts =>
        {
            opts.Discovery.IncludeAssembly(typeof(IdentityServiceExtensions).Assembly);
            opts.Discovery.IncludeAssembly(typeof(ReceptionServiceExtensions).Assembly);
        });
    }
}
