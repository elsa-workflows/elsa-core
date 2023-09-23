using System.Reflection;
using System.Runtime.Loader;
using NuGet.Packaging;

namespace Elsa.DropIns.Contexts;

internal class NuGetPackageAssemblyLoadContext : AssemblyLoadContext
{
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new Dictionary<string, Assembly>();

    public NuGetPackageAssemblyLoadContext(string nugetPackagePath)
    {
        var packageReader = new PackageArchiveReader(nugetPackagePath);

        foreach (var dllFile in packageReader.GetFiles().Where(fileName => fileName.EndsWith(".dll")))
        {
            using var dllStream = packageReader.GetStream(dllFile);
            using var memoryStream = new MemoryStream();
            dllStream.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var assembly = LoadFromStream(memoryStream);

            _loadedAssemblies[assembly.FullName!] = assembly;
        }
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        return _loadedAssemblies.TryGetValue(assemblyName.FullName, out var assembly) ? assembly : null;
    }
}