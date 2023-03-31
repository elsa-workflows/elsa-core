using System.Reflection;

namespace Elsa.Workflows.Core.Contracts;

public interface IPropertyDefaultValueResolver
{
    object? GetDefaultValue(PropertyInfo activityPropertyInfo);
}