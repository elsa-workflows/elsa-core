using System.Reflection;

namespace Elsa.Workflows.Contracts;

public interface IActivityPropertyDefaultValueProvider
{
    object GetDefaultValue(PropertyInfo property);
}