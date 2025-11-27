using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Extensions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Api.Serialization;

/// <summary>
/// Converts <see cref="ArgumentDefinition"/> derivatives from and to JSON
/// </summary>
public class ArgumentJsonConverter : JsonConverter<ArgumentDefinition>
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    /// <inheritdoc />
    public ArgumentJsonConverter(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ArgumentDefinition value, JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.RemoveWhere(x => x is ArgumentJsonConverterFactory);

        var jsonObject = (JsonObject)JsonSerializer.SerializeToNode(value, value.GetType(), newOptions)!;
        var typeName = value.Type;
        var typeAlias = _wellKnownTypeRegistry.TryGetAlias(typeName, out var alias) ? alias : null;
        var isArray = typeName.IsArray;
        var isCollection = typeName.IsCollectionType();
        var elementTypeName = isArray ? typeName.GetElementType()! : isCollection ? typeName.GenericTypeArguments[0] : typeName;
        var elementTypeAlias = _wellKnownTypeRegistry.GetAliasOrDefault(elementTypeName);
        var isAliasedArray = (isArray || isCollection) && typeAlias != null;
        var finalTypeAlias = isArray || isCollection ? typeAlias ?? elementTypeAlias : elementTypeAlias;

        if (isArray && !isAliasedArray) jsonObject["isArray"] = isArray;
        if (isCollection) jsonObject["isCollection"] = isCollection;

        jsonObject["type"] = finalTypeAlias;
        JsonSerializer.Serialize(writer, jsonObject, newOptions);
    }

    /// <inheritdoc />
    [UnconditionalSuppressMessage("Trimming", "IL2055:Call to MakeGenericType can not be statically analyzed", Justification = "Types are dynamically resolved from workflow definitions and registered in the well-known type registry.")]
    public override ArgumentDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonObject = (JsonObject)JsonNode.Parse(ref reader)!;
        var isArray = jsonObject["isArray"]?.GetValue<bool>() ?? false;
        var isCollection = jsonObject["isCollection"]?.GetValue<bool>() ?? false;
        var typeAlias = jsonObject["type"]!.GetValue<string>();
        var type = _wellKnownTypeRegistry.GetTypeOrDefault(typeAlias);

        if (isArray) type = type.MakeArrayType();
        if (isCollection) type = type.MakeGenericType(type.GenericTypeArguments[0]);

        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.RemoveWhere(x => x is ArgumentJsonConverterFactory);
        var inputDefinition = (ArgumentDefinition)jsonObject.Deserialize(typeToConvert, newOptions)!;
        inputDefinition.Type = type;

        return inputDefinition;
    }
}