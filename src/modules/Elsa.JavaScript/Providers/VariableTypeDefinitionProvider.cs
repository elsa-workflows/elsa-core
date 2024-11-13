using System.Dynamic;
using Elsa.Extensions;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.Providers;

/// <summary>
/// Produces <see cref="TypeDefinition"/>s for variable types.
/// </summary>
internal class VariableTypeDefinitionProvider(ITypeDescriber typeDescriber) : TypeDefinitionProvider
{
    protected override IEnumerable<TypeDefinition> GetTypeDefinitions(TypeDefinitionContext context)
    {
        var excludedTypes = new Func<Type, bool>[]
        {
            type => type == typeof(ExpandoObject),
            type => typeof(IDictionary<string, object>).IsAssignableFrom(type),
            type => type == typeof(object)
        };

        var variables = context.WorkflowGraph.Workflow.Variables;

        var variableTypeQuery =
            from variable in variables
            let variableType = variable.GetVariableType()
            where (variableType.IsClass || variableType.IsInterface) && !variableType.IsPrimitive && !excludedTypes.Any(x => x(variableType))
            select variableType;

        var variableTypes = variableTypeQuery.Distinct();

        foreach (var variableType in variableTypes)
        {
            yield return typeDescriber.DescribeType(variableType);
        }
    }
}