using System.Reflection;

namespace Elsa.Metadata
{
    public interface IActivityPropertyOptionsResolver
    {
        object? GetOptions(PropertyInfo activityPropertyInfo);
    }
}