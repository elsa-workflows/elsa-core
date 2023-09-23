using System.Reflection;
using System.Runtime.Loader;
using Elsa.DropIns.Contexts;

namespace Elsa.DropIns.Helpers;

public static class AssemblyLoader
{
    public static Assembly LoadPath(string path)
    {
        var loadContext = new DirectoryAssemblyLoadContext(path);
        var assemblyName = AssemblyLoadContext.GetAssemblyName(path);
        var assembly = loadContext.LoadFromAssemblyName(assemblyName);

        return assembly;
    }
}