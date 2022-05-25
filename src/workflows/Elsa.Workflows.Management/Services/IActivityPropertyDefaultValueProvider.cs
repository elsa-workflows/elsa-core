using System.Reflection;

namespace Elsa.Workflows.Management.Services;

public interface IActivityPropertyDefaultValueProvider
{
    object GetDefaultValue(PropertyInfo property);
}