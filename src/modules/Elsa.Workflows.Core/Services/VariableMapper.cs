using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Maps variables to and from <see cref="VariableModel"/> instances.
/// </summary>
public class VariableMapper
{
    private readonly ILogger<VariableMapper> _logger;
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableMapper"/> class.
    /// </summary>
    /// <param name="wellKnownTypeRegistry">The well-known type registry.</param>
    /// <param name="logger">The logger.</param>
    public VariableMapper(IWellKnownTypeRegistry wellKnownTypeRegistry, ILogger<VariableMapper> logger)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
        _logger = logger;
    }

    /// <inheritdoc />
    public VariableMapper() : this(new WellKnownTypeRegistry(), NullLogger<VariableMapper>.Instance)
    {
        
    }

    /// <summary>
    /// Maps a <see cref="Variable"/> to a <see cref="VariableModel"/>.
    /// </summary>
    public Variable Map(VariableModel source)
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

    /// <summary>
    /// Maps a <see cref="VariableModel"/> to a <see cref="Variable"/>.
    /// </summary>
    public VariableModel Map(Variable source)
    {
        var variableType = source.GetType();
        var value = source.Value;
        var valueType = variableType.IsConstructedGenericType ? variableType.GetGenericArguments().FirstOrDefault() ?? typeof(object) : typeof(object);
        var valueTypeAlias = _wellKnownTypeRegistry.GetAliasOrDefault(valueType);
        var storageDriverTypeName = source.StorageDriverType?.GetSimpleAssemblyQualifiedName();
        var serializedValue = value.Format();

        return new VariableModel(source.Id, source.Name, valueTypeAlias, serializedValue, storageDriverTypeName);
    }
}