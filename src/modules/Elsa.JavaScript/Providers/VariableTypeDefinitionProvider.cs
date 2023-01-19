using System.Dynamic;
using Elsa.Extensions;
using Elsa.JavaScript.Abstractions;
using Elsa.JavaScript.Models;
using Elsa.JavaScript.Services;

namespace Elsa.JavaScript.Providers;

/// <summary>
/// Produces <see cref="TypeDefinition"/>s for variable types.
/// </summary>
internal class VariableTypeDefinitionProvider : TypeDefinitionProvider
{
    private readonly ITypeDescriber _typeDescriber;

    public VariableTypeDefinitionProvider(ITypeDescriber typeDescriber)
    {
        _typeDescriber = typeDescriber;
    }

    protected override IEnumerable<TypeDefinition> GetTypeDefinitions(TypeDefinitionContext context)
    {
        var excludedTypes = new[] { typeof(ExpandoObject), typeof(IDictionary<string, object>) };

        var variableTypeQuery =
            from variable in context.Variables
            let variableType = variable.GetVariableType()
            where (variableType.IsClass || variableType.IsInterface) && !variableType.IsPrimitive && !excludedTypes.Any(x => x.IsAssignableFrom(variableType))
            select variableType;

        var variableTypes = variableTypeQuery.Distinct();
        
        foreach (var variableType in variableTypes)
        {
            yield return _typeDescriber.DescribeType(variableType);
        }
    }
}