using System.Reflection;
using Elsa.DropIns.Core;

namespace Elsa.DropIns;

public class DropInStartupFinder : IDropInStartupFinder
{
    public IEnumerable<Type> FindDropInStartupTypes(IEnumerable<Assembly> assemblies)
    {
        // Find all types that implement IDropInStartup:
        return assemblies.SelectMany(FindDropInStartupTypes);
    }

    public IEnumerable<Type> FindDropInStartupTypes(Assembly assembly)
    {
        // Find all types that implement IDropInStartup:
        return assembly.GetExportedTypes().Where(t => typeof(IDropInStartup).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });
    }
}