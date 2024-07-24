using Elsa.DropIns.Contracts;
using Elsa.DropIns.Models;

namespace Elsa.DropIns.Catalogs;

public class NuGetPackagesCatalog(IEnumerable<string> packagePaths) : IDropInCatalog
{
    private readonly IEnumerable<string> _packagePaths = packagePaths;

    public IEnumerable<DropInDescriptor> List() 
        => _packagePaths.Select(packagePath => new NuGetPackageCatalog(packagePath)).SelectMany(catalog => catalog.List());
}