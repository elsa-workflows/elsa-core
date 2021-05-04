using System;
using Newtonsoft.Json;

namespace Elsa.Client.Converters
{
    public class TypeConverter : JsonConverter<Type>
    {
        public override void WriteJson(JsonWriter writer, Type? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value!.AssemblyQualifiedName);
        }

        public override Type ReadJson(JsonReader reader, Type objectType, Type? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var typeName = serializer.Deserialize<string>(reader)!;
            return Type.GetType(typeName)!;
        }
    }
}