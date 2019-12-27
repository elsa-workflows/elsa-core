using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public interface IValueHandler
    {
        int Priority { get; }
        bool CanSerialize(object value, JToken token, Type type);
        bool CanDeserialize(JToken token, Type type);
        object? Deserialize(JsonSerializer serializer, Type type, JToken token);
        void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken token, object value);
    }
}