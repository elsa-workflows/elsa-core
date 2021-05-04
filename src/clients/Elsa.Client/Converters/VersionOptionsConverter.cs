using System;
using Elsa.Client.Models;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Elsa.Client.Converters
{
    public class VersionOptionsConverter : JsonConverter<VersionOptions>
    {
        public override void WriteJson(JsonWriter writer, VersionOptions value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToString());
        }

        public override VersionOptions ReadJson(JsonReader reader, Type objectType, VersionOptions existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var text = serializer.Deserialize<string>(reader)!;
            return VersionOptions.FromString(text);
        }
    }
}