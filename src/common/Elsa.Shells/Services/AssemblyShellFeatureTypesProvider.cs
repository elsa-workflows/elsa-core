using System.Reflection;

namespace Elsa.Shells.Services;

public class AssemblyShellFeatureTypesProvider : IShellFeatureTypesProvider
{
    private ICollection<Type>? _featureTypes;

    public IEnumerable<Type> GetFeatureTypes()
    {
        return _featureTypes ??= GetFeatureTypesInternal().ToList();
    }

    private IEnumerable<Type> GetFeatureTypesInternal()
    {
        // Scan all referenced assemblies for types implementing IShellFeature.
        var featureTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IShellFeature).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .ToList();

        return featureTypes;
    }

    private IEnumerable<Type> GetFeatureTypes(Assembly assembly)
    {
        return assembly.GetTypes().Where(type => typeof(IShellFeature).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);
    }
}