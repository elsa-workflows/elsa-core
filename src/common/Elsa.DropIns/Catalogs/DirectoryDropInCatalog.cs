using System.Reflection;
using System.Runtime.Loader;
using Elsa.DropIns.Contexts;
using Elsa.DropIns.Contracts;
using Elsa.DropIns.Models;

namespace Elsa.DropIns.Catalogs;

public class DirectoryDropInCatalog : IDropInCatalog
{
    private readonly string _directoryPath;

    public DirectoryDropInCatalog(string directoryPath)
    {
        _directoryPath = directoryPath;
    }

    public IEnumerable<DropInDescriptor> List()
    {
        var assemblies = ListAssemblies();
        var assembliesCatalog = new AssembliesDropInCatalog(assemblies);
        
        return assembliesCatalog.List();
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

    private Assembly? LoadDropInAssembly(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
            return null;

        var loadContext = new DropInLoadContext(assemblyPath);
        var assemblyName = AssemblyLoadContext.GetAssemblyName(assemblyPath);
        var assembly = loadContext.LoadFromAssemblyName(assemblyName);

        return assembly;
    }
}