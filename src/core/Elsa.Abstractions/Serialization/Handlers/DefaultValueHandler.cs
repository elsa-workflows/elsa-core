using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public class DefaultValueHandler : IValueHandler
    {
        public int Priority => -9000;
        public bool CanSerialize(JToken value, Type type) => true;
        public bool CanDeserialize(JToken value, Type type) => true;
        public object Deserialize(JsonReader reader, JsonSerializer serializer, Type type, JToken value) => serializer.Deserialize(value.CreateReader(), type);
        public void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken value) => serializer.Serialize(writer, value);
    }
}