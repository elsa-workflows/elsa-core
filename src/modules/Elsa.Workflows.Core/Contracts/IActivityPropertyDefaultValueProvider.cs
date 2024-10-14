using System.Reflection;

namespace Elsa.Workflows;

public interface IActivityPropertyDefaultValueProvider
{
    object GetDefaultValue(PropertyInfo property);
}