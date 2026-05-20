using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Workflows.Serialization.Helpers;
using JetBrains.Annotations;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Type"/> objects to a simple alias representing the type.
/// </summary>
[UsedImplicitly]
public class TypeJsonConverter : JsonConverter<Type>
{
    private const string UnregisteredTypeAliasPrefix = "UnregisteredClrType:";
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    /// <inheritdoc />
    public TypeJsonConverter(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
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

        return WorkflowJsonTypeResolver.ResolveType(_wellKnownTypeRegistry, typeAlias);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        if (!WorkflowJsonTypeResolver.TryGetAlias(_wellKnownTypeRegistry, value, out var typeAlias))
            typeAlias = $"{UnregisteredTypeAliasPrefix}{value.GetSimpleAssemblyQualifiedName()}";

        writer.WriteStringValue(typeAlias);
    }
}
