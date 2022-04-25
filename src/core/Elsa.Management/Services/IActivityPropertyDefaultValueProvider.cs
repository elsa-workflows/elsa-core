using System.Reflection;

namespace Elsa.Management.Services;

public interface IActivityPropertyDefaultValueProvider
{
    object GetDefaultValue(PropertyInfo property);
}