using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Options;
using Elsa.Extensions;
using Elsa.Workflows.Serialization.Helpers;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Type"/> objects to a simple alias representing the type.
/// </summary>
[UsedImplicitly]
public class TypeJsonConverter : JsonConverter<Type>
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;
    private readonly bool _allowLegacyClrTypeNames;

    /// <inheritdoc />
    public TypeJsonConverter(IWellKnownTypeRegistry wellKnownTypeRegistry)
        : this(wellKnownTypeRegistry, true)
    {
    }

    /// <inheritdoc />
    public TypeJsonConverter(IWellKnownTypeRegistry wellKnownTypeRegistry, IOptions<ExpressionOptions> expressionOptions)
        : this(wellKnownTypeRegistry, expressionOptions.Value.AllowLegacyClrTypeNames)
    {
    }

    /// <inheritdoc />
    public TypeJsonConverter(IWellKnownTypeRegistry wellKnownTypeRegistry, bool allowLegacyClrTypeNames)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
        _allowLegacyClrTypeNames = allowLegacyClrTypeNames;
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

        return WorkflowJsonTypeResolver.ResolveType(_wellKnownTypeRegistry, typeAlias, _allowLegacyClrTypeNames);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        if (!WorkflowJsonTypeResolver.TryGetAlias(_wellKnownTypeRegistry, value, out var typeAlias))
        {
            if (!_allowLegacyClrTypeNames)
                throw new JsonException($"Type '{value}' is not registered as a workflow JSON type alias.");

            typeAlias = value.GetSimpleAssemblyQualifiedName();
        }

        writer.WriteStringValue(typeAlias);
    }
}
