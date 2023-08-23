using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// A JSON converter factory that creates <see cref="SafeDictionaryConverter"/> instances.
/// </summary>
public class SafeDictionaryConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        // Check if the type is assignable to IDictionary<string, object>
        return typeof(IDictionary<string, object>).IsAssignableFrom(typeToConvert);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (CanConvert(typeToConvert))
            return new SafeDictionaryConverter();

        throw new InvalidOperationException($"Cannot convert {typeToConvert}");
    }
}