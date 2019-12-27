using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public class DefaultValueHandler : IValueHandler
    {
        public int Priority => -9000;
        public bool CanSerialize(object value, JToken token, Type type) => true;
        public bool CanDeserialize(JToken token, Type type) => true;
        public object? Deserialize(JsonSerializer serializer, Type type, JToken token) => serializer.Deserialize(token.CreateReader(), type);
        public void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken token, object value) => serializer.Serialize(writer, token);
    }
}