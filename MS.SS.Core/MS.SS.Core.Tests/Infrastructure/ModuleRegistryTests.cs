using MS.SS.Core.Infrastructure.Database.Config;

namespace MS.SS.Core.Tests.Infrastructure;

public class ModuleRegistryTests
{
    [Fact]
    public void Every_registered_module_assembly_can_be_loaded()
    {
        var assemblies = ModuleAssemblyLoader.LoadAll().ToList();

        Assert.Equal(ModuleRegistry.AssemblyNames.Count, assemblies.Count);
    }

    [Fact]
    public void Reception_schema_is_named_after_its_module()
    {
        Assert.Equal("reception", ModuleSchemas.Reception);
    }
}
