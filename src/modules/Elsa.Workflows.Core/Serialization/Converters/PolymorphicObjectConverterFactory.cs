using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// A JSON converter factory that creates <see cref="PolymorphicObjectConverter"/> instances.
/// </summary>
public class PolymorphicObjectConverterFactory(IWellKnownTypeRegistry wellKnownTypeRegistry) : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert.IsClass
               && typeToConvert == typeof(object)
               || typeToConvert == typeof(ExpandoObject)
               || typeToConvert == typeof(Dictionary<string, object>))
            return true;

        if (typeToConvert.IsInterface
               && typeToConvert == typeof(IDictionary<string, object>))
            return true;

        return false;
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeof(IDictionary<string, object>).IsAssignableFrom(typeToConvert))
            return new PolymorphicDictionaryConverter(options, wellKnownTypeRegistry);

        return new PolymorphicObjectConverter(wellKnownTypeRegistry);
    }
}