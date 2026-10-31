using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using JetBrains.Annotations;
using Elsa.Common.Serialization;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Type"/> objects to a simple alias representing the type.
/// Unregistered types are written as metadata-only aliases and intentionally deserialize to <see cref="Exception"/> instead of loading the original CLR type.
/// </summary>
[UsedImplicitly]
public class TypeJsonConverter : JsonConverter<Type>
{
    /// <summary>
    /// Prefix for unregistered type metadata that is not used for CLR type loading during deserialization.
    /// </summary>
    private const string UnregisteredTypeAliasPrefix = "UnregisteredClrType:";
    private readonly ISerializationTypeRegistry _workflowJsonTypeRegistry;

    /// <inheritdoc />
    public TypeJsonConverter(ISerializationTypeRegistry workflowJsonTypeRegistry)
    {
        _workflowJsonTypeRegistry = workflowJsonTypeRegistry;
    }

    /// <inheritdoc />
    public TypeJsonConverter()
    {
        _workflowJsonTypeRegistry = SerializationTypeRegistry.CreateDefault();
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(Type) || typeToConvert.FullName == "System.RuntimeType";
    }

    /// <inheritdoc />
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var typeAlias = reader.GetString();
        if (typeAlias?.StartsWith(UnregisteredTypeAliasPrefix, StringComparison.Ordinal) == true)
            return typeof(Exception);

        return SerializationTypeResolver.ResolveType(_workflowJsonTypeRegistry, typeAlias);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        if (!SerializationTypeResolver.TryGetAlias(_workflowJsonTypeRegistry, value, out var typeAlias))
            typeAlias = $"{UnregisteredTypeAliasPrefix}{value.GetSimpleAssemblyQualifiedName()}";

        writer.WriteStringValue(typeAlias);
    }
}
