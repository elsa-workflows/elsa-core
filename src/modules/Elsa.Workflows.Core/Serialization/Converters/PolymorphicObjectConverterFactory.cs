using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Services;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// A JSON converter factory that creates <see cref="PolymorphicObjectConverter"/> instances.
/// </summary>
public class PolymorphicObjectConverterFactory : JsonConverterFactory
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    /// <summary>
    /// A JSON converter factory that creates <see cref="PolymorphicObjectConverter"/> instances.
    /// </summary>
    public PolymorphicObjectConverterFactory(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }

    /// <summary>
    /// Default constructor for use with attributes.
    /// </summary>
    public PolymorphicObjectConverterFactory()
    {
        _wellKnownTypeRegistry = WellKnownTypeRegistry.CreateDefault();
    }

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
            return new PolymorphicDictionaryConverter(options, _wellKnownTypeRegistry);

        return new PolymorphicObjectConverter(_wellKnownTypeRegistry);
    }
}