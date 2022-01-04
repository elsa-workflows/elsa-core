using System.Reflection;

namespace Elsa.Management.Contracts;

public interface IActivityPropertyDefaultValueProvider
{
    object GetDefaultValue(PropertyInfo property);
}