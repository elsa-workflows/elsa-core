using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Management.Mappers;

/// <summary>
/// Maps <see cref="Variable"/>s to <see cref="VariableDefinition"/>s and vice versa.
/// </summary>
public class VariableDefinitionMapper
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    /// <summary>
    /// Constructor.
    /// </summary>
    public VariableDefinitionMapper(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }

    /// <summary>
    /// Maps a <see cref="VariableDefinition"/> to a <see cref="Variable"/>.
    /// </summary>
    public Variable? Map(VariableDefinition source)
    {
        if (!_wellKnownTypeRegistry.TryGetTypeOrDefault(source.TypeName, out var type))
            return null;

        var valueType = source.IsArray ? typeof(ICollection<>).MakeGenericType(type) : type;
        var variableGenericType = typeof(Variable<>).MakeGenericType(valueType);
        var variable = (Variable)Activator.CreateInstance(variableGenericType)!;

        if(!string.IsNullOrEmpty(source.Id))
            variable.Id = source.Id;
        
        variable.Name = source.Name;
        variable.Value = source.Value.ConvertTo(valueType);
        variable.StorageDriverType = !string.IsNullOrEmpty(source.StorageDriverTypeName) ? Type.GetType(source.StorageDriverTypeName) : default;

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
        ?? Enumerable.Empty<Variable>();

    /// <summary>
    /// Maps a <see cref="Variable"/> to a <see cref="VariableDefinition"/>.
    /// </summary>
    public VariableDefinition Map(Variable source)
    {
        var variableType = source.GetType();
        var valueType = variableType.IsConstructedGenericType ? variableType.GetGenericArguments().FirstOrDefault() ?? typeof(object) : typeof(object);
        var isArray = valueType.IsCollectionType();
        var elementValueType = isArray ? valueType.GenericTypeArguments[0] : valueType; 
        var value = source.Value;
        
        var valueTypeAlias = _wellKnownTypeRegistry.GetAliasOrDefault(elementValueType);
        var storageDriverTypeName = source.StorageDriverType?.GetSimpleAssemblyQualifiedName();
        var serializedValue = value.Format();

        return new VariableDefinition(source.Id, source.Name, valueTypeAlias, isArray, serializedValue, storageDriverTypeName);
    }

    /// <summary>
    /// Maps a list of <see cref="Variable"/>s to a list of <see cref="VariableDefinition"/>s.
    /// </summary>
    public IEnumerable<VariableDefinition> Map(IEnumerable<Variable>? source) => source?.Select(Map) ?? Enumerable.Empty<VariableDefinition>();
}