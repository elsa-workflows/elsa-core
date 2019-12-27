using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public interface IValueHandler
    {
        int Priority { get; }
        bool CanSerialize(JToken token, Type type, object value);
        bool CanDeserialize(JToken token);
        object? Deserialize(JsonSerializer serializer, JToken token);
        void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken token, object value);
    }
}