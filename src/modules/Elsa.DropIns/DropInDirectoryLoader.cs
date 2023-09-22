using System.Reflection;
using System.Runtime.Loader;

namespace Elsa.DropIns;

public class DropInDirectoryLoader : IDropInDirectoryLoader
{
    public IEnumerable<Assembly> LoadAssembliesFromDirectory(string directoryPath)
    {
        var loadContext = new DropInLoadContext(directoryPath);
        //var loadContext = AssemblyLoadContext.Default;
        
        var assemblyPaths = Directory.GetFiles(directoryPath, "*.dll", new EnumerationOptions
        {
            RecurseSubdirectories = true
        }).Where(path => !Path.GetFileName(path).StartsWith("Elsa.")); // Exclude Elsa assemblies to prevent assembly identity issues. As an alternative, we could use the default load context.
        var assemblies = assemblyPaths.Select(loadContext.LoadFromAssemblyPath);
        return assemblies.ToList();
    }
}