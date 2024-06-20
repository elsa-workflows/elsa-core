using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// A JSON converter factory that creates <see cref="PolymorphicObjectConverter"/> instances.
/// </summary>
public class PolymorphicObjectConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PolymorphicObjectConverterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
            return new PolymorphicDictionaryConverter(_serviceProvider, options);

        return new PolymorphicObjectConverter(_serviceProvider);
    }
}