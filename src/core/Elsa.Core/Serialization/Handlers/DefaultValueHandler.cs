using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public class DefaultValueHandler : IValueHandler
    {
        public int Priority => -9000;
        public bool CanSerialize(JToken token, Type type, object value) => true;
        public bool CanDeserialize(JToken token) => true;
        public object? Deserialize(JsonSerializer serializer, JToken token) => serializer.Deserialize(token.CreateReader());
        public void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken token, object value) => serializer.Serialize(writer, token);
    }
}