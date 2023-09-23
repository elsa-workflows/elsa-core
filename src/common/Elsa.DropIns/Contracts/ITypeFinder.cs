using System.Reflection;

namespace Elsa.DropIns.Contracts;

public interface ITypeFinder
{
    IEnumerable<Type> FindImplementationsOf<T>(IEnumerable<Assembly> assemblies);
    IEnumerable<Type> FindImplementationsOf<T>(Assembly assembly);
}

