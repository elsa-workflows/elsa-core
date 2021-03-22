using System.Reflection;

namespace Elsa.Metadata
{
    public interface IActivityPropertyDefaultValueResolver
    {
        object? GetDefaultValue(PropertyInfo activityPropertyInfo);
    }
}