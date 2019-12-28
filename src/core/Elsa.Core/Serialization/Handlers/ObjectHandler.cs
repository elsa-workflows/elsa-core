using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public class ObjectHandler : IValueHandler
    {
        private readonly ITypeMap typeMap;
        private const string TypeFieldName = "typeName";

        public ObjectHandler(ITypeMap typeMap)
        {
            this.typeMap = typeMap;
        }
        
        public int Priority => -8999;
        public bool CanSerialize(JToken token, Type type, object value) => token.Type == JTokenType.Object;
        public bool CanDeserialize(JToken token) => token.Type == JTokenType.Object;
        
        public object Deserialize(JsonSerializer serializer, JToken token)
        {
            var typeName = token[TypeFieldName]?.Value<string>();
            
            if(typeName == null)
                throw new InvalidOperationException();
            
            var objectType = typeMap.GetType(typeName);
            return token.ToObject(objectType, serializer);
        }

        public void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken token, object? value)
        {
            token[TypeFieldName] = typeMap.GetAlias(type);
            token.WriteTo(writer, serializer.Converters.ToArray());
        }
    }
}