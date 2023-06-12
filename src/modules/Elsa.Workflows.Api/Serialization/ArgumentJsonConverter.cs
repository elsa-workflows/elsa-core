using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Extensions;
using Elsa.Workflows.Core.Models;

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
        var isArray = value.Type.IsCollectionType();
        jsonObject["isArray"] = isArray;
        jsonObject["type"] = _wellKnownTypeRegistry.GetAliasOrDefault(isArray ? value.Type.GetCollectionElementType() : value.Type);

        JsonSerializer.Serialize(writer, jsonObject, newOptions);
    }

    /// <inheritdoc />
    public override ArgumentDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonObject = (JsonObject)JsonNode.Parse(ref reader)!;
        var isArray = jsonObject["isArray"]?.GetValue<bool>() ?? false;
        var typeName = jsonObject["type"]!.GetValue<string>();
        var type = _wellKnownTypeRegistry.GetTypeOrDefault(typeName);

        if (isArray)
            type = type.MakeCollectionType();

        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.RemoveWhere(x => x is ArgumentJsonConverterFactory);
        var inputDefinition = (ArgumentDefinition)jsonObject.Deserialize(typeToConvert, newOptions)!;
        inputDefinition.Type = type;

        return inputDefinition;
    }
}