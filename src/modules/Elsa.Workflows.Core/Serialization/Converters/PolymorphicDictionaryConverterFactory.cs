using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// A JSON converter factory that creates <see cref="PolymorphicDictionaryConverter"/> instances.
/// </summary>
public class PolymorphicDictionaryConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(IDictionary<string, object>).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new PolymorphicDictionaryConverter(options);
    }
}