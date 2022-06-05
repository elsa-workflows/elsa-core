using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elsa.Expressions.Helpers;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Api.Mappers;

public class VariableDefinitionMapper
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    public VariableDefinitionMapper(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }

    public Variable? Map(VariableDefinition source)
    {
        if (!_wellKnownTypeRegistry.TryGetTypeOrDefault(source.Type, out var type))
            return null;

        var variableGenericType = typeof(Variable<>).MakeGenericType(type);
        var variable = (Variable)Activator.CreateInstance(variableGenericType)!;

        variable.Name = source.Name;
        variable.Value = source.Value.ConvertTo(type);

        return variable;
    }

    public IEnumerable<Variable> Map(IEnumerable<VariableDefinition>? source) =>
        source?
            .Select(Map)
            .Where(x => x != null)
            .Select(x => x!)
        ?? Enumerable.Empty<Variable>();

    public VariableDefinition Map(Variable source)
    {
        var variableType = source.GetType();
        var value = source.Value;
        var valueType = source.Value?.GetType() ?? (variableType.IsConstructedGenericType ? variableType.GetGenericArguments().FirstOrDefault() ?? typeof(object) : typeof(object));
        var valueTypeAlias = _wellKnownTypeRegistry.GetAliasOrDefault(valueType);

        var serializedValue = value.Format();

        return new VariableDefinition(source.Name, valueTypeAlias, serializedValue);
    }

    public IEnumerable<VariableDefinition> Map(IEnumerable<Variable>? source) => source?.Select(Map) ?? Enumerable.Empty<VariableDefinition>();
}