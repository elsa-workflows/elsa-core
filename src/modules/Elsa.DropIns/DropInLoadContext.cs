using System.Reflection;
using System.Runtime.Loader;

namespace Elsa.DropIns;

internal class DropInLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public DropInLoadContext(string dropInPath)
    {
        _resolver = new AssemblyDependencyResolver(dropInPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Check if the assembly is already loaded in the default context.
        var defaultAssembly = Default.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
        
        if (defaultAssembly != null)
            return defaultAssembly;

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}