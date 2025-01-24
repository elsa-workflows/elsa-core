using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Management.Mappers;

/// <summary>
/// Maps <see cref="Variable"/>s to <see cref="VariableDefinition"/>s and vice versa.
/// </summary>
public class VariableDefinitionMapper(IWellKnownTypeRegistry wellKnownTypeRegistry, ILogger<VariableDefinitionMapper> logger)
{
    /// <summary>
    /// Maps a <see cref="VariableDefinition"/> to a <see cref="Variable"/>.
    /// </summary>
    public Variable? Map(VariableDefinition source)
    {
        if (!wellKnownTypeRegistry.TryGetTypeOrDefault(source.TypeName, out var type))
            return null;

        var valueType = source.IsArray ? type.MakeArrayType() : type;
        var variableGenericType = typeof(Variable<>).MakeGenericType(valueType);
        var variable = (Variable)Activator.CreateInstance(variableGenericType)!;

        if(!string.IsNullOrEmpty(source.Id))
            variable.Id = source.Id;
        
        variable.Name = source.Name;
        source.Value?.TryConvertTo(valueType).OnSuccess(value =>
        {
            variable.Value = value;
        }).OnFailure(ex =>
        {
            logger.LogWarning(ex, "Failed to convert the default value {DefaultValue} of variable {VariableName} to its type {VariableType}. Default value will not be set.", source.Value, source.Name, valueType);
        });
        variable.StorageDriverType = !string.IsNullOrEmpty(source.StorageDriverTypeName) ? Type.GetType(source.StorageDriverTypeName) : null;

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
        var isArray = valueType.IsArray;
        var isCollection = valueType.IsCollectionType();
        var elementValueType = isArray ? valueType.GetElementType() : isCollection ? valueType.GenericTypeArguments[0] : valueType; 
        var value = source.Value;
        var valueTypeAlias = wellKnownTypeRegistry.GetAliasOrDefault(elementValueType);
        var storageDriverTypeName = source.StorageDriverType?.GetSimpleAssemblyQualifiedName();
        var serializedValue = value.Format();

        return new(source.Id, source.Name, valueTypeAlias, isArray, serializedValue, storageDriverTypeName);
    }

    /// <summary>
    /// Maps a list of <see cref="Variable"/>s to a list of <see cref="VariableDefinition"/>s.
    /// </summary>
    public IEnumerable<VariableDefinition> Map(IEnumerable<Variable>? source) => source?.Select(Map) ?? [];
}