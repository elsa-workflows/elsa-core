using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// A JSON converter factory that creates <see cref="PolymorphicObjectConverter"/> instances.
/// </summary>
public class PolymorphicObjectConverterFactory : JsonConverterFactory
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;
    private readonly bool _allowLegacyClrTypeNames;

    /// <summary>
    /// A JSON converter factory that creates <see cref="PolymorphicObjectConverter"/> instances.
    /// </summary>
    public PolymorphicObjectConverterFactory(IWellKnownTypeRegistry wellKnownTypeRegistry)
        : this(wellKnownTypeRegistry, true)
    {
    }

    /// <summary>
    /// A JSON converter factory that creates <see cref="PolymorphicObjectConverter"/> instances.
    /// </summary>
    public PolymorphicObjectConverterFactory(IWellKnownTypeRegistry wellKnownTypeRegistry, IOptions<ExpressionOptions> expressionOptions)
        : this(wellKnownTypeRegistry, expressionOptions.Value.AllowLegacyClrTypeNames)
    {
    }

    /// <summary>
    /// A JSON converter factory that creates <see cref="PolymorphicObjectConverter"/> instances.
    /// </summary>
    public PolymorphicObjectConverterFactory(IWellKnownTypeRegistry wellKnownTypeRegistry, bool allowLegacyClrTypeNames)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
        _allowLegacyClrTypeNames = allowLegacyClrTypeNames;
    }

    /// <summary>
    /// Default constructor for use with attributes.
    /// </summary>
    public PolymorphicObjectConverterFactory()
        : this(WellKnownTypeRegistry.CreateDefault(), true)
    {
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

        return new PolymorphicObjectConverter(_wellKnownTypeRegistry, _allowLegacyClrTypeNames);
    }
}
