using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Client.Models;

namespace Elsa.Client.Converters
{
    public class VersionOptionsConverter : JsonConverter<VersionOptions>
    {
        public override VersionOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => VersionOptions.FromString(reader.GetString());
        public override void Write(Utf8JsonWriter writer, VersionOptions value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
    }
}