using Elsa.DropIns.Contexts;
using Elsa.DropIns.Contracts;
using Elsa.DropIns.Models;

namespace Elsa.DropIns.Catalogs;

public class NuGetPackageCatalog : IDropInCatalog
{
    private readonly string _packagePath;

    public NuGetPackageCatalog(string packagePath)
    {
        _packagePath = packagePath;
    }

    public IEnumerable<DropInDescriptor> List()
    {
        var assemblyLoadContext = new NuGetPackageAssemblyLoadContext(_packagePath);
        var assemblies = assemblyLoadContext.Assemblies;
        var assembliesCatalog = new AssembliesDropInCatalog(assemblies);
        
        return assembliesCatalog.List();
    }
}