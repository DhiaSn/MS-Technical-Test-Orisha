namespace MS.SS.Core.Infrastructure.Database.Config;

/// <summary>
/// The modules whose EF configurations <see cref="Context.AppDbContext"/> must apply.
/// </summary>
/// <remarks>
/// Infrastructure sits below the modules in the dependency graph (they reference it, not the
/// other way round), so it cannot name their types. Listing assembly names here is the seam that
/// keeps that direction intact.
/// </remarks>
public static class ModuleRegistry
{
    public const string IdentityAssembly = "MS.SS.Core.Modules.Identity";
    public const string ReceptionAssembly = "MS.SS.Core.Modules.Reception";

    public static IReadOnlyList<string> AssemblyNames => [IdentityAssembly, ReceptionAssembly];
}
