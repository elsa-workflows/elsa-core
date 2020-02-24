using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Elsa.Metadata
{
    public interface IActivityPropertyOptionsProvider
    {
        bool SupportsProperty(PropertyInfo property);
        void SupplyOptions(PropertyInfo property, JObject options);
    }
}