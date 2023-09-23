using System.Reflection;

namespace Elsa.DropIns.Contracts;

public interface IDropInDirectoryLoader
{
    IEnumerable<Assembly> LoadDropInAssembliesFromRootDirectory(string rootDirectoryPath);
    Assembly? LoadDropInAssembly(string assemblyPath);
}