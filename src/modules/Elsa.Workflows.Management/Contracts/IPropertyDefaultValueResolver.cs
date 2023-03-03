using System.Reflection;

namespace Elsa.Workflows.Management.Contracts;

public interface IPropertyDefaultValueResolver
{
    object? GetDefaultValue(PropertyInfo activityPropertyInfo);
}