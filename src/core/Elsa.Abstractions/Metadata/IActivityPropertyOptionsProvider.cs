using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Elsa.Metadata
{
    public interface IActivityPropertyOptionsProvider
    {
        object GetOptions(PropertyInfo property);
    }
}