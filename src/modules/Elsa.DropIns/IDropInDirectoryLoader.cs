using System.Reflection;

namespace Elsa.DropIns;

public interface IDropInDirectoryLoader
{
    IEnumerable<Assembly> LoadAssembliesFromDirectory(string directoryPath);
}