using System.Reflection;

namespace Elsa.Management.Contracts;

public interface IPropertyOptionsResolver
{
    object? GetOptions(PropertyInfo propertyInfo);
}