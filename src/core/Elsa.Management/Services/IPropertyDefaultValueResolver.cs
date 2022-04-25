using System.Reflection;

namespace Elsa.Management.Services;

public interface IPropertyDefaultValueResolver
{
    object? GetDefaultValue(PropertyInfo activityPropertyInfo);
}