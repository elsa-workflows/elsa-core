using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.Core.Models;
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
    public override Variable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Add(new JsonPrimitiveToStringConverter());
        var model = JsonSerializer.Deserialize<VariableModel>(ref reader, newOptions)!;
        var variable = Map(model);

        return variable;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Variable value, JsonSerializerOptions options)
    {
        var model = Map(value);
        JsonSerializer.Serialize(writer, model, options);
    }

    private Variable Map(VariableModel source)
    {
        var typeName = source.TypeName;
        
        if (string.IsNullOrWhiteSpace(source.TypeName))
            typeName = _wellKnownTypeRegistry.GetAliasOrDefault(typeof(object));

        if (!_wellKnownTypeRegistry.TryGetTypeOrDefault(typeName, out var type))
            type = typeof(object);

        var variableGenericType = typeof(Variable<>).MakeGenericType(type);
        var variable = (Variable)Activator.CreateInstance(variableGenericType)!;

        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        variable.Id = source.Id ?? Guid.NewGuid().ToString("N"); // Temporarily assign a new ID if the source doesn't have one.
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

        return new VariableModel(source.Id, source.Name, valueTypeAlias, serializedValue, storageDriverTypeName);
    }

    private class VariableModel
    {
        [JsonConstructor]
        public VariableModel()
        {
        }

        public VariableModel(string id, string name, string typeName, string? value, string? storageDriverTypeName)
        {
            Id = id;
            Name = name;
            TypeName = typeName;
            Value = value;
            StorageDriverTypeName = storageDriverTypeName;
        }

        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string TypeName { get; set; } = default!;
        public string? Value { get; set; }
        public string? StorageDriverTypeName { get; set; }
    }
}