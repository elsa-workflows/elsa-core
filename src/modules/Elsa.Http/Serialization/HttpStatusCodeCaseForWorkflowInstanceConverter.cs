using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Http.Models;

namespace Elsa.Http.Serialization;

/// <summary>
/// A custom JSON converter for <see cref="HttpStatusCodeCase"/> objects when serializing workflow states.
/// </summary>
public class HttpStatusCodeCaseForWorkflowInstanceConverter : JsonConverter<HttpStatusCodeCase>
{
    /// <inheritdoc />
    public override HttpStatusCodeCase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, HttpStatusCodeCase value, JsonSerializerOptions options)
    {
        var properties = value.GetType().GetProperties();

        writer.WriteStartObject();

        foreach (var prop in properties)
        {
            // Don't serialize the Activity property.
            if (prop.Name != nameof(HttpStatusCodeCase.Activity))
            {
                var propValue = prop.GetValue(value);
                writer.WritePropertyName(prop.Name);
                JsonSerializer.Serialize(writer, propValue, prop.PropertyType, options);
            }
        }

        writer.WriteEndObject();
    }
}