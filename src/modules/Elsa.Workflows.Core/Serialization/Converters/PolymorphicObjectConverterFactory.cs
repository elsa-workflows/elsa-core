using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common.Serialization;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// A JSON converter factory that creates <see cref="PolymorphicObjectConverter"/> instances.
/// </summary>
public class PolymorphicObjectConverterFactory : JsonConverterFactory
{
    private readonly ISerializationTypeRegistry _workflowJsonTypeRegistry;

    /// <summary>
    /// A JSON converter factory that creates <see cref="PolymorphicObjectConverter"/> instances.
    /// </summary>
    public PolymorphicObjectConverterFactory(ISerializationTypeRegistry workflowJsonTypeRegistry)
    {
        _workflowJsonTypeRegistry = workflowJsonTypeRegistry;
    }

    /// <summary>
    /// Default constructor for use with attributes.
    /// </summary>
    public PolymorphicObjectConverterFactory()
    {
        _workflowJsonTypeRegistry = SerializationTypeRegistry.CreateDefault();
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
            return new PolymorphicDictionaryConverter(options, _workflowJsonTypeRegistry);

        return new PolymorphicObjectConverter(_workflowJsonTypeRegistry);
    }
}
