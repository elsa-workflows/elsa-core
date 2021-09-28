using System.Reflection;

namespace Elsa.Metadata
{
    public interface IActivityPropertyOptionsProvider
    {
        object? GetOptions(PropertyInfo property);
    }
}