using Wolverine;

namespace MS.SS.Core.API.Extensions;

public static class WolverineExtensions
{
    public static void AddAppWolverine(this WebApplicationBuilder builder)
    {
        builder.Host.UseWolverine(opts =>
        {
        });
    }
}
