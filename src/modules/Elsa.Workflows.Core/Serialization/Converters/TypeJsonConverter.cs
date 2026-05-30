using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Serialization.Helpers;
using Elsa.Workflows.Serialization.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Type"/> objects to a simple alias representing the type.
/// </summary>
[UsedImplicitly]
public class TypeJsonConverter : JsonConverter<Type>
{
    private readonly WorkflowJsonOptions _workflowJsonOptions;

    /// <inheritdoc />
    public TypeJsonConverter()
        : this(new WorkflowJsonOptions())
    {
    }

    /// <inheritdoc />
    public TypeJsonConverter(IOptions<WorkflowJsonOptions> workflowJsonOptions)
        : this(workflowJsonOptions.Value)
    {
    }

    /// <inheritdoc />
    public TypeJsonConverter(WorkflowJsonOptions workflowJsonOptions)
    {
        _workflowJsonOptions = workflowJsonOptions;
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

        return WorkflowJsonTypeResolver.ResolveType(_workflowJsonOptions, typeAlias, _workflowJsonOptions.AllowLegacyClrTypeNames);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        var typeAlias = WorkflowJsonTypeResolver.GetAliasOrLegacyClrTypeName(_workflowJsonOptions, value);
        writer.WriteStringValue(typeAlias);
    }
}
