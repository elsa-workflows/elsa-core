using System.Reflection;

namespace Elsa.Management.Implementations;

public interface IActivityPropertyOptionsProvider
{
    object? GetOptions(PropertyInfo property);
}