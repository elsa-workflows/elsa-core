using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Elsa.Serialization.Converters
{
    /// <summary>
    /// Correctly deserializes a <see cref="Stack{T}"/> maintaining the order of the items.
    /// </summary>
    public class StackJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var newSerializer = new JsonSerializer();
            newSerializer.Serialize(writer, value);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var newSerializer = new JsonSerializer();
            var list = newSerializer.Deserialize(reader, objectType)!;
            return Activator.CreateInstance(objectType, list);
        }

        public override bool CanConvert(Type objectType) => objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Stack<>);
    }
}