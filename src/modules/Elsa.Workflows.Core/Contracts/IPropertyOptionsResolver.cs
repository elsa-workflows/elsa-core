using System.Reflection;

namespace Elsa.Workflows.Core.Contracts;

public interface IPropertyOptionsResolver
{
    object? GetOptions(PropertyInfo propertyInfo);
}