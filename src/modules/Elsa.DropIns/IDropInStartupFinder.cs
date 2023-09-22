using System.Reflection;

namespace Elsa.DropIns;

public interface IDropInStartupFinder
{
    IEnumerable<Type> FindDropInStartupTypes(IEnumerable<Assembly> assemblies);
    IEnumerable<Type> FindDropInStartupTypes(Assembly assembly);
}