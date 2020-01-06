using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public class ObjectHandler : IValueHandler
    {
        private const string TypeFieldName = "TypeName";
        public int Priority => -8999;
        public bool CanSerialize(JToken value, Type type) => value.Type == JTokenType.Object;
        public bool CanDeserialize(JToken value, Type type) => value.Type == JTokenType.Object;
        
        public object Deserialize(JsonReader reader, JsonSerializer serializer, Type type, JToken value)
        {
            var typeName = value[TypeFieldName].Value<string>();
            var objectType = Type.GetType(typeName);
            return value.ToObject(objectType, serializer);
        }

        public void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken value)
        {
            value[TypeFieldName] = GetAssemblyQualifiedTypeName(type);
            value.WriteTo(writer, serializer.Converters.ToArray());
        }
        
        private string GetAssemblyQualifiedTypeName(Type type)
        {
            var typeName = type.FullName;
            var assemblyName = type.Assembly.GetName().Name;

            return $"{typeName}, {assemblyName}";
        }
    }
}