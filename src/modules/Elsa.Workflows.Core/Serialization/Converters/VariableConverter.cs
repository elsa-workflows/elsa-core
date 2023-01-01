using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Type"/> objects to a simple alias representing said type.
/// </summary>
public class VariableConverter : JsonConverter<Variable>
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;
    private readonly ILogger _logger;

    /// <inheritdoc />
    public VariableConverter(IWellKnownTypeRegistry wellKnownTypeRegistry, ILogger<VariableConverter> logger)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
        _logger = logger;
    }

    /// <inheritdoc />
    public override Variable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var model = JsonSerializer.Deserialize<VariableModel>(ref reader, options)!;
        var variable = Map(model);

        return variable;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Variable value, JsonSerializerOptions options)
    {
        var model = Map(value);
        JsonSerializer.Serialize(writer, model, options);
    }

    private Variable? Map(VariableModel source)
    {
        if (string.IsNullOrWhiteSpace(source.TypeName))
            return null;

        if (!_wellKnownTypeRegistry.TryGetTypeOrDefault(source.TypeName, out var type))
            return null;

        var variableGenericType = typeof(Variable<>).MakeGenericType(type);
        var variable = (Variable)Activator.CreateInstance(variableGenericType)!;

        variable.Name = source.Name;

        source.Value.TryConvertTo(type)
            .OnSuccess(value => variable.Value = value)
            .OnFailure(e => _logger.LogWarning("Failed to convert {SourceValue} to {TargetType}", source.Value, type.Name));

        variable.StorageDriverType = !string.IsNullOrEmpty(source.StorageDriverTypeName) ? Type.GetType(source.StorageDriverTypeName) : default;

        return variable;
    }

    private VariableModel Map(Variable source)
    {
        var variableType = source.GetType();
        var value = source.Value;
        var valueType = variableType.IsConstructedGenericType ? variableType.GetGenericArguments().FirstOrDefault() ?? typeof(object) : typeof(object);
        var valueTypeAlias = _wellKnownTypeRegistry.GetAliasOrDefault(valueType);
        var storageDriverTypeName = source.StorageDriverType?.GetSimpleAssemblyQualifiedName();
        var serializedValue = value.Format();

        return new VariableModel(source.Name, valueTypeAlias, serializedValue, storageDriverTypeName);
    }

    private class VariableModel
    {
        [JsonConstructor]
        public VariableModel()
        {
        }

        public VariableModel(string name, string typeName, string? value, string? storageDriverTypeName)
        {
            Name = name;
            TypeName = typeName;
            Value = value;
            StorageDriverTypeName = storageDriverTypeName;
        }

        public string Name { get; set; } = default!;
        public string TypeName { get; set; } = default!;
        public string? Value { get; set; }
        public string? StorageDriverTypeName { get; set; }
    }
}