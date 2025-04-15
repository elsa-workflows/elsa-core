using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Management.Mappers;

/// <summary>
/// Maps <see cref="Variable"/>s to <see cref="VariableDefinition"/>s and vice versa.
/// </summary>
public class VariableDefinitionMapper(IWellKnownTypeRegistry wellKnownTypeRegistry, IServiceScopeFactory scopeFactory, ILogger<VariableDefinitionMapper> logger)
{
    /// <summary>
    /// Maps a <see cref="VariableDefinition"/> to a <see cref="Variable"/>.
    /// </summary>
    public Variable? Map(VariableDefinition source)
    {
        var aliasedType = wellKnownTypeRegistry.TryGetType(source.TypeName, out var aliasedTypeValue) ? aliasedTypeValue : null;
        var type = aliasedType ?? Type.GetType(source.TypeName);

        if (type == null)
        {
            logger.LogWarning("Failed to resolve the type {TypeName} of variable {VariableName}. Variable will not be mapped.", source.TypeName, source.Name);
            return null;
        }

        var valueType = aliasedType is { IsArray: true } ? source.IsArray ? aliasedType.MakeArrayType() : aliasedType : source.IsArray ? type.MakeArrayType() : type;
        var variableGenericType = typeof(Variable<>).MakeGenericType(valueType);
        var variable = (Variable)Activator.CreateInstance(variableGenericType)!;

        if (!string.IsNullOrEmpty(source.Id))
            variable.Id = source.Id;

        variable.Name = source.Name;

        if (!string.IsNullOrWhiteSpace(source.Value))
        {
            source.Value?.TryConvertTo(valueType).OnSuccess(value =>
            {
                variable.Value = value;
            }).OnFailure(ex =>
            {
                logger.LogWarning(ex, "Failed to convert the default value {DefaultValue} of variable {VariableName} to its type {VariableType}. Default value will not be set.", source.Value, source.Name, valueType);
            });
        }

        variable.StorageDriverType = GetStorageDriverType(source.StorageDriverTypeName);

        return variable;
    }

    /// <summary>
    /// Maps a list of <see cref="VariableDefinition"/>s to a list of <see cref="Variable"/>.
    /// </summary>
    public IEnumerable<Variable> Map(IEnumerable<VariableDefinition>? source) =>
        source?
            .Select(Map)
            .Where(x => x != null)
            .Select(x => x!)
        ?? [];

    /// <summary>
    /// Maps a <see cref="Variable"/> to a <see cref="VariableDefinition"/>.
    /// </summary>
    public VariableDefinition Map(Variable source)
    {
        var variableType = source.GetType();
        var valueType = variableType.IsConstructedGenericType ? variableType.GetGenericArguments().FirstOrDefault() ?? typeof(object) : typeof(object);
        var valueTypeAlias = wellKnownTypeRegistry.TryGetAlias(valueType, out var alias) ? alias : null;
        var value = source.Value;
        var serializedValue = value.Format();
        var storageDriverTypeName = source.StorageDriverType?.GetSimpleAssemblyQualifiedName();

        // Handles the case where an alias exists for an array or collection type. E.g. byte[] -> ByteArray.
        if (valueTypeAlias != null && (valueType.IsArray || valueType.IsCollectionType()))
            return new(source.Id, source.Name, valueTypeAlias, false, serializedValue, storageDriverTypeName);

        var isArray = valueType.IsArray;
        var isCollection = valueType.IsCollectionType();
        var elementValueType = isArray ? valueType.GetElementType() : isCollection ? valueType.GenericTypeArguments[0] : valueType;
        var elementTypeAlias = wellKnownTypeRegistry.GetAliasOrDefault(elementValueType);

        return new(source.Id, source.Name, elementTypeAlias, isArray, serializedValue, storageDriverTypeName);
    }

    /// <summary>
    /// Maps a list of <see cref="Variable"/>s to a list of <see cref="VariableDefinition"/>s.
    /// </summary>
    public IEnumerable<VariableDefinition> Map(IEnumerable<Variable>? source) => source?.Select(Map) ?? [];
    
    private Type? GetStorageDriverType(string? storageDriverTypeName)
    {
        if (string.IsNullOrEmpty(storageDriverTypeName))
            return null;

        var type = Type.GetType(storageDriverTypeName);

        if (type != null)
            return type;

        // TODO: The following code handles backward compatibility with variable definitions referencing older .NET type namespaces.
        // We will refactor this by storing a driver identifier rather than its full type name - which is brittle in case we move namespaces.
        using var scope = scopeFactory.CreateScope();
        var storageDrivers = scope.ServiceProvider.GetServices<IStorageDriver>();
        foreach (var storageDriver in storageDrivers)
        {
            var typeName = storageDriver.GetType().Name;
            if (storageDriverTypeName.Contains(typeName, StringComparison.OrdinalIgnoreCase))
                return storageDriver.GetType();
        }

        return null;
    }
}