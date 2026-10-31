using Elsa.DropIns.Contracts;
using Elsa.DropIns.Models;

namespace Elsa.DropIns.Catalogs;

public class NuGetPackagesCatalog : IDropInCatalog
{
    private readonly IEnumerable<string> _packagePaths;

    public NuGetPackagesCatalog(IEnumerable<string> packagePaths)
    {
        _packagePaths = packagePaths;
    }
    
    public IEnumerable<DropInDescriptor> List()
    {
        return _packagePaths.Select(packagePath => new NuGetPackageCatalog(packagePath)).SelectMany(catalog => catalog.List());
    }
}