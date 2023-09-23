using System.Reflection;
using Elsa.DropIns.Contracts;

namespace Elsa.DropIns.Services;

public class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> FindImplementationsOf<T>(IEnumerable<Assembly> assemblies)
    {
        // Find all types that implement IDropInStartup:
        return assemblies.SelectMany(FindImplementationsOf<T>);
    }

    public IEnumerable<Type> FindImplementationsOf<T>(Assembly assembly)
    {
        // Find all types that implement T:
        return assembly.GetExportedTypes().Where(t => typeof(T).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });
    }
}