using System.Reflection;

namespace Elsa.Metadata
{
    public interface IActivityPropertyDefaultValueProvider
    {
        object GetDefaultValue(PropertyInfo property);
    }
}