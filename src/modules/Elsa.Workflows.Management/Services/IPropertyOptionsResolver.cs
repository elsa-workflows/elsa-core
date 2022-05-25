using System.Reflection;

namespace Elsa.Workflows.Management.Services;

public interface IPropertyOptionsResolver
{
    object? GetOptions(PropertyInfo propertyInfo);
}