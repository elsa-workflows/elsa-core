using System.Linq;
using System.Reflection;
using Elsa.Attributes;
using Elsa.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Metadata.Handlers
{
    public class SelectOptionsProvider : IActivityPropertyOptionsProvider
    {
        private readonly JsonSerializer serializer;

        public SelectOptionsProvider(ITokenSerializerProvider serializerProvider)
        {
            serializer = serializerProvider.CreateJsonSerializer();
        }
        
        public bool SupportsProperty(PropertyInfo property) => property.GetCustomAttribute<SelectOptionsAttribute>() != null || property.PropertyType.IsEnum;

        public void SupplyOptions(PropertyInfo property, JObject options)
        {
            var attr = property.GetCustomAttribute<SelectOptionsAttribute>();
            
            if(attr != null)
                options["Items"] = JToken.FromObject(attr.GetOptions(), serializer);
            else
            {
                var enumValues = property.PropertyType.GetEnumNames().OrderBy(x => x);
                var items = enumValues.Select(x => new SelectOption(x)).ToList();
                options["Items"] = JToken.FromObject(items, serializer);
            }
        }
    }
}