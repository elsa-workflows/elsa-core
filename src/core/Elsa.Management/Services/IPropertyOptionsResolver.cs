using System.Reflection;

namespace Elsa.Management.Services;

public interface IPropertyOptionsResolver
{
    object? GetOptions(PropertyInfo propertyInfo);
}