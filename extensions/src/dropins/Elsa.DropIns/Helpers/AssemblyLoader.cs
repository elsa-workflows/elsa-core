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
        
        // Copy drop in to memory stream to avoid file locking
        using var fileStream = File.OpenRead(path);
        using var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        fileStream.Close();
        var assembly = loadContext.LoadFromStream(memoryStream);

        return assembly;
    }
}