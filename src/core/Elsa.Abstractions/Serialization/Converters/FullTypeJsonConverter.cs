using System;
using Newtonsoft.Json;

namespace Elsa.Serialization.Converters
{
    public class FullTypeJsonConverter : JsonConverter<Type>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, Type? value, JsonSerializer serializer)
        {
            var typeName = value!.AssemblyQualifiedName;
            serializer.Serialize(writer, typeName);
        }

        public override Type ReadJson(JsonReader reader, Type objectType, Type? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var typeName = serializer.Deserialize<string>(reader)!;
            return typeName is not null and not "" ? Type.GetType(typeName)! : default!;
        }
    }
}