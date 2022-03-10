using System;
using Elsa.Models;
using Elsa.Serialization;
using Newtonsoft.Json;

namespace Elsa.Server.Api.Converters;

/// <summary>
/// Serializes just the <see cref="Variables.Data"/> property.
/// </summary>
public class VariablesConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(Variables);
    public override bool CanRead => false;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var variables = (Variables)(value ?? new Variables());
        var variablesSerializerSettings = DefaultContentSerializer.CreateDefaultJsonSerializationSettings();
        variablesSerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.None;
        var json = JsonConvert.SerializeObject(variables.Data, Formatting.Indented, variablesSerializerSettings);
        serializer.Serialize(writer, json);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}