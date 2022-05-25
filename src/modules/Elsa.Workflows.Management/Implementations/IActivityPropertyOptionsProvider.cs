using System.Reflection;

namespace Elsa.Workflows.Management.Implementations;

public interface IActivityPropertyOptionsProvider
{
    object? GetOptions(PropertyInfo property);
}