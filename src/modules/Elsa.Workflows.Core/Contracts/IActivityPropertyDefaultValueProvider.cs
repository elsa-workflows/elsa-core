using System.Reflection;

namespace Elsa.Workflows.Core.Contracts;

public interface IActivityPropertyDefaultValueProvider
{
    object GetDefaultValue(PropertyInfo property);
}