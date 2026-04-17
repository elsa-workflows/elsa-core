using Nuplane.Loading;

namespace Elsa.ModularServer.Web.Catalog;

/// <summary>
/// Sample-only query service that explicitly discovers plugin types from the current active package set,
/// layered on top of the assembly catalog convenience surface.
/// </summary>
internal sealed class PluginCatalog(
    IPackageAssemblyCatalog packageAssemblyCatalog,
    IPackageTypeFinder packageTypeFinder)
{
    private readonly IPackageAssemblyCatalog _packageAssemblyCatalog = packageAssemblyCatalog ?? throw new ArgumentNullException(nameof(packageAssemblyCatalog));
    private readonly IPackageTypeFinder _packageTypeFinder = packageTypeFinder ?? throw new ArgumentNullException(nameof(packageTypeFinder));

    /// <summary>
    /// Discovers all currently scanable <see cref="IPlugin"/> implementations from active loaded packages.
    /// </summary>
    public async Task<IReadOnlyList<DiscoveredPluginDescriptor>> DiscoverAsync(CancellationToken cancellationToken)
    {
        var discovered = new List<DiscoveredPluginDescriptor>();

        foreach (var package in (await _packageAssemblyCatalog.GetPackagedAssembliesAsync(cancellationToken))
                     .Where(static package => package.AssemblyReferences.Count > 0))
        {
            var pluginTypes = (await _packageTypeFinder.FindTypesAsync(typeof(IPlugin), package.PackageId, cancellationToken))
                .OrderBy(static pluginType => pluginType.FullName, StringComparer.Ordinal)
                .ToArray();

            foreach (var pluginType in pluginTypes)
            {
                discovered.Add(new(
                    package.PackageId,
                    package.Version,
                    pluginType.FullName ?? pluginType.Name,
                    pluginType.Assembly.GetName().Name ?? pluginType.Assembly.FullName ?? "<unknown>",
                    package.AssemblyReferences.Select(static candidate => candidate.AssemblyFileName).ToArray()));
            }
        }

        return discovered;
    }
}

internal sealed record DiscoveredPluginDescriptor(
    string PackageId,
    string Version,
    string PluginType,
    string AssemblyName,
    IReadOnlyList<string> ScanCandidateAssemblyFileNames);

