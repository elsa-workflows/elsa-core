using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public class ObjectHandler : IValueHandler
    {
        private const string TypeFieldName = "typeName";
        public int Priority => -8999;
        public bool CanSerialize(JToken token, Type type, object value) => token.Type == JTokenType.Object;
        public bool CanDeserialize(JToken token) => token.Type == JTokenType.Object;
        
        public object Deserialize(JsonSerializer serializer, JToken token)
        {
            var typeName = token[TypeFieldName]?.Value<string>();
            
            if(typeName == null)
                throw new InvalidOperationException();
            
            var objectType = Type.GetType(typeName);
            return token.ToObject(objectType, serializer);
        }

        public void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken token, object? value)
        {
            token[TypeFieldName] = GetAssemblyQualifiedTypeName(type);
            token.WriteTo(writer, serializer.Converters.ToArray());
        }
        
        private string GetAssemblyQualifiedTypeName(Type type)
        {
            var typeName = type.FullName;
            var assemblyName = type.Assembly.GetName().Name;

            return $"{typeName}, {assemblyName}";
        }
    }
}