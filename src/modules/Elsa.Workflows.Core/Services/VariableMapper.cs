using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Workflows;

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
        var type = ResolveVariableType(source.TypeName);

        var variableGenericType = typeof(Variable<>).MakeGenericType(type);
        var variable = (Variable)Activator.CreateInstance(variableGenericType)!;

        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        variable.Id = source.Id ?? Guid.NewGuid().ToString("N"); // Temporarily assign a new ID if the source doesn't have one.
        variable.Name = source.Name;

        source.Value.TryConvertTo(type)
            .OnSuccess(value => variable.Value = value)
            .OnFailure(e => _logger.LogWarning("Failed to convert {SourceValue} to {TargetType}", source.Value, type.Name));

        variable.StorageDriverType = ResolveStorageDriverType(source.StorageDriverTypeName);

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

        return new(source.Id, source.Name, valueTypeAlias, serializedValue, storageDriverTypeName);
    }

    private Type ResolveVariableType(string? typeAlias)
    {
        if (string.IsNullOrWhiteSpace(typeAlias))
            return typeof(object);

        if (WorkflowJsonTypeResolver.TryResolveType(_wellKnownTypeRegistry, typeAlias, out var type))
            return type;

        _logger.LogWarning("Failed to resolve variable type alias {VariableTypeName}", typeAlias);
        return typeof(object);
    }

    private Type? ResolveStorageDriverType(string? typeAlias)
    {
        if (string.IsNullOrWhiteSpace(typeAlias))
            return null;

        if (WorkflowJsonTypeResolver.TryResolveType(_wellKnownTypeRegistry, typeAlias, out var type))
            return type;

        _logger.LogWarning("Failed to resolve storage driver type alias {StorageDriverTypeName}", typeAlias);
        return null;
    }
}
