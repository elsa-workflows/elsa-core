using System;
using Elsa.Models;
using Newtonsoft.Json;

namespace Elsa.Serialization.Converters
{
    public class VersionOptionsJsonConverter : JsonConverter<VersionOptions>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override void WriteJson(JsonWriter writer, VersionOptions value, JsonSerializer serializer) => serializer.Serialize(writer, value.ToString());
        public override VersionOptions ReadJson(JsonReader reader, Type objectType, VersionOptions existingValue, bool hasExistingValue, JsonSerializer serializer) => VersionOptions.FromString(serializer.Deserialize<string>(reader)!);
    }
}