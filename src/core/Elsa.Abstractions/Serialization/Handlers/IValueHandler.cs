using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public interface IValueHandler
    {
        int Priority { get; }
        bool CanSerialize(JToken value, Type type);
        bool CanDeserialize(JToken value, Type type);
        object Deserialize(JsonReader reader, JsonSerializer serializer, Type type, JToken value);
        void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken value);
    }
}