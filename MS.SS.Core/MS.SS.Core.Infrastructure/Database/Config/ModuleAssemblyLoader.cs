using System.Reflection;

namespace MS.SS.Core.Infrastructure.Database.Config;

/// <summary>
/// Loads module assemblies by name so the DbContext can apply their entity configurations at both
/// runtime and design time (<c>dotnet ef</c>, where the module assemblies are not yet loaded).
/// </summary>
public static class ModuleAssemblyLoader
{
    public static IEnumerable<Assembly> LoadAll()
    {
        foreach (var name in ModuleRegistry.AssemblyNames)
        {
            var assembly = Load(name);

            if (assembly is null)
            {
                // Failing loudly matters: a module whose configurations silently fail to load
                // produces a migration that drops its tables.
                throw new InvalidOperationException(
                    $"Module assembly '{name}' could not be loaded. Its entity configurations would be " +
                    "missing from the model, which would generate a migration that drops its tables. " +
                    "Check that the module is referenced by MS.SS.Core.API and MS.SS.Core.App.");
            }

            yield return assembly;
        }
    }

    private static Assembly? Load(string assemblyName)
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);

        if (loaded is not null) return loaded;

        try
        {
            return Assembly.Load(assemblyName);
        }
        catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException)
        {
            // Design-time fallback: probe next to the Infrastructure assembly.
            var directory = Path.GetDirectoryName(typeof(ModuleAssemblyLoader).Assembly.Location);

            if (string.IsNullOrEmpty(directory)) return null;

            var path = Path.Combine(directory, $"{assemblyName}.dll");

            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }
    }
}
