using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// A JSON converter factory that creates <see cref="ExpandoObjectConverter"/> instances.
/// </summary>
public class ExpandoObjectConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        var canConvert = typeToConvert.IsClass && typeToConvert == typeof(object);
        
        return canConvert;
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new ExpandoObjectConverter();
    }
}