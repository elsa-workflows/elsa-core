using System;
using System.Linq;
using System.Reflection;
using Elsa.Attributes;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Elsa.Metadata.Handlers
{
    public class SelectOptionsProvider : IActivityPropertyOptionsProvider
    {
        private readonly Func<JsonSerializer> _createSerializer;

        public SelectOptionsProvider(Func<JsonSerializer> serializerFactory)
        {
            _createSerializer = serializerFactory;
        }
        
        public bool SupportsProperty(PropertyInfo property) => property.GetCustomAttribute<SelectOptionsAttribute>() != null || property.PropertyType.IsEnum;

        public void SupplyOptions(PropertyInfo property, JObject options)
        {
            var attr = property.GetCustomAttribute<SelectOptionsAttribute>();
            var serializer = _createSerializer();
            
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