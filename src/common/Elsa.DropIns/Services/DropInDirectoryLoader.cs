using System.Reflection;
using System.Runtime.Loader;
using Elsa.DropIns.Contexts;
using Elsa.DropIns.Contracts;

namespace Elsa.DropIns.Services;

public class DropInDirectoryLoader : IDropInDirectoryLoader
{
    public IEnumerable<Assembly> LoadDropInAssembliesFromRootDirectory(string rootDirectoryPath)
    {
        var directoryPaths = Directory.GetDirectories(rootDirectoryPath);

        foreach (var directoryPath in directoryPaths)
        {
            var dropInAssemblyFilename = $"{Path.GetFileNameWithoutExtension(directoryPath)}.dll";
            var assemblyPath = Path.Combine(directoryPath, dropInAssemblyFilename);
            var assembly = LoadDropInAssembly(assemblyPath);

            if (assembly == null)
                continue;

            yield return assembly;
        }
    }
    
    public Assembly? LoadDropInAssembly(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
            return null;

        var loadContext = new DropInLoadContext(assemblyPath);
        var assemblyName = AssemblyLoadContext.GetAssemblyName(assemblyPath);
        var assembly = loadContext.LoadFromAssemblyName(assemblyName);

        return assembly;
    }
}