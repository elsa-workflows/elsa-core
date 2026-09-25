using System.Reflection;

namespace Elsa.DropIns.Helpers;

public static class TypeFinder
{
    public static IEnumerable<Type> FindImplementationsOf<T>(IEnumerable<Assembly> assemblies)
    {
        // Find all types that implement IDropInStartup:
        return assemblies.SelectMany(FindImplementationsOf<T>);
    }

    public static IEnumerable<Type> FindImplementationsOf<T>(Assembly assembly)
    {
        // Find all types that implement T:
        return assembly.GetExportedTypes().Where(t => typeof(T).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });
    }
}