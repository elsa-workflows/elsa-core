using System.Reflection;
using Elsa.DropIns.Contracts;
using Elsa.DropIns.Helpers;
using Elsa.DropIns.Models;

namespace Elsa.DropIns.Catalogs;

/// <summary>
/// A catalog that lists drop-ins from a directory.
/// </summary>
public class DirectoryDropInCatalog : IDropInCatalog
{
    private readonly string _directoryPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryDropInCatalog"/> class.
    /// </summary>
    /// <param name="directoryPath">The directory path to list drop-ins from.</param>
    public DirectoryDropInCatalog(string directoryPath)
    {
        _directoryPath = directoryPath;
    }

    /// <inheritdoc />
    public IEnumerable<DropInDescriptor> List()
    {
        if (!Directory.Exists(_directoryPath))
            return Enumerable.Empty<DropInDescriptor>();

        var assemblies = ListAssemblies();
        var packages = ListPackages();
        var assembliesCatalog = new AssembliesDropInCatalog(assemblies);
        var packagesCatalog = new NuGetPackagesCatalog(packages);

        return assembliesCatalog.List().Concat(packagesCatalog.List());
    }

    private IEnumerable<Assembly> ListAssemblies()
    {
        var assemblyPaths = Directory.GetFiles(_directoryPath, "*.dll", SearchOption.AllDirectories);

        foreach (var assemblyPath in assemblyPaths)
        {
            var assembly = LoadDropInAssembly(assemblyPath);

            if (assembly == null)
                continue;

            yield return assembly;
        }
    }

    private IEnumerable<string> ListPackages()
    {
        return Directory.GetFiles(_directoryPath, "*.nupkg", SearchOption.AllDirectories);
    }

    private Assembly? LoadDropInAssembly(string path)
    {
        return !File.Exists(path) ? null : AssemblyLoader.LoadPath(path);
    }
}