using System.Reflection;
using Elsa.DropIns.Contracts;
using Elsa.DropIns.Models;

namespace Elsa.DropIns.Catalogs;

public class AssembliesDropInCatalog(IEnumerable<Assembly> assemblies) : IDropInCatalog
{
    private readonly IEnumerable<Assembly> _assemblies = assemblies;

    public IEnumerable<DropInDescriptor> List()
    {
        var allDropIns = new List<DropInDescriptor>();
        
        foreach (var assembly in _assemblies)
        {
            var assemblyCatalog = new AssemblyDropInCatalog(assembly);
            var dropIns = assemblyCatalog.List();
            allDropIns.AddRange(dropIns);
        }
        
        return allDropIns;
    }
}