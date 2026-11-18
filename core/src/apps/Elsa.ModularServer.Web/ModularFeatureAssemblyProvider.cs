using System.Reflection;
using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyModel;
using Nuplane.Loading;

namespace Elsa.ModularServer.Web;

/// <summary>Provides the host and Nuplane assemblies used for shell activation and startup validation.</summary>
[UsedImplicitly]
internal sealed class ModularFeatureAssemblyProvider(IPackageAssemblyCatalog packageLoader) : IFeatureAssemblyProvider
{
    public async Task<IEnumerable<Assembly>> GetAssembliesAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var hostAssemblies = GetHostAssemblies().ToList();
        var packageAssemblies = await packageLoader.GetAssembliesAsync(cancellationToken);
        return hostAssemblies.Concat(packageAssemblies).DistinctBy(assembly => assembly.FullName, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<Assembly> GetHostAssemblies()
    {
        // Match CShells WithHostAssemblies: discover entry and runtime dependency assemblies by simple name.
        var context = DependencyContext.Default;
        var runtimeNames = context?.RuntimeLibraries.SelectMany(library => library.GetDefaultAssemblyNames(context)) ?? [];
        var names = runtimeNames.Prepend(Assembly.GetEntryAssembly()?.GetName()).OfType<AssemblyName>()
            .DistinctBy(name => name.Name, StringComparer.OrdinalIgnoreCase);
        var loadedAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (loaded.GetName().Name is { } simpleName)
                loadedAssemblies[simpleName] = loaded;
        }

        foreach (var name in names)
        {
            if (name.Name is not { } simpleName)
                continue;

            if (loadedAssemblies.TryGetValue(simpleName, out var assembly))
            {
                yield return assembly;
                continue;
            }

            try
            {
                assembly = Assembly.Load(name);
            }
            catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
            {
                // Optional dependencies may not be loadable in this process.
                continue;
            }

            yield return assembly;
        }
    }
}
