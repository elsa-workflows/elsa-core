using System.Reflection;

namespace Elsa.Workflows.Management.Contracts;

public interface IPropertyOptionsResolver
{
    object? GetOptions(PropertyInfo propertyInfo);
}