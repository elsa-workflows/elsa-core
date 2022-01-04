using System.Reflection;

namespace Elsa.Management.Contracts;

public interface IPropertyDefaultValueResolver
{
    object? GetDefaultValue(PropertyInfo activityPropertyInfo);
}