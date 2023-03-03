using System.Reflection;

namespace Elsa.Workflows.Management.Contracts;

public interface IActivityPropertyDefaultValueProvider
{
    object GetDefaultValue(PropertyInfo property);
}