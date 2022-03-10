using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Elsa.Models;
using Elsa.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        
        // Convert ExpandoObject to JArray/JObject.
        object? ProcessVariable(object? v)
        {
            if (v is ExpandoObject expandoObject)
            {
                return JObject.FromObject(expandoObject);
            }

            if (v is ICollection<ExpandoObject> expandoObjects)
            {
                return new JArray(expandoObjects.Select(JObject.FromObject).Cast<object>().ToArray());
            }

            return v;
        }
        
        var variablesDictionary = variables.Data.ToDictionary(x => x.Key, x => ProcessVariable(x.Value));
        
        var json = JsonConvert.SerializeObject(variablesDictionary, Formatting.Indented, variablesSerializerSettings);
        serializer.Serialize(writer, json);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}