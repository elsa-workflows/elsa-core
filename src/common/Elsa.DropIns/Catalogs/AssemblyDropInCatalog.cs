using System.Reflection;
using Elsa.DropIns.Contracts;
using Elsa.DropIns.Core;
using Elsa.DropIns.Helpers;
using Elsa.DropIns.Models;

namespace Elsa.DropIns.Catalogs;

public class AssemblyDropInCatalog(Assembly assembly) : IDropInCatalog
{
    private readonly Assembly _assembly = assembly;

    public IEnumerable<DropInDescriptor> List()
    {
        var dropIns = TypeFinder.FindImplementationsOf<IDropIn>(_assembly)
            .Select(type => new DropInDescriptor(type))
            .ToList();

        return dropIns;
    }
}