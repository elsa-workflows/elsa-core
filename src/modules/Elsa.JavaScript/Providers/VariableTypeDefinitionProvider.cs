using System.Dynamic;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Workflows.Management.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.JavaScript.Providers;

/// <summary>
/// Produces <see cref="TypeDefinition"/>s for variable types.
/// </summary>
[UsedImplicitly]
internal class VariableTypeDefinitionProvider(ITypeDescriber typeDescriber, IOptions<ManagementOptions> options) : TypeDefinitionProvider
{
    protected override IEnumerable<TypeDefinition> GetTypeDefinitions(TypeDefinitionContext context)
    {
        var excludedTypes = new Func<Type, bool>[]
        {
            type => type == typeof(ExpandoObject),
            type => type.IsPrimitive,
            type => type.ContainsGenericParameters,
            type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>),
            type => type == typeof(object),
            type => type == typeof(string)
        };

        var variableTypes =
            from variableDescriptor in options.Value.VariableDescriptors
            let variableType = variableDescriptor.Type
            where (variableType.IsClass || variableType.IsInterface || variableType.IsEnum) && !excludedTypes.Any(x => x(variableType))
            select variableType;

        foreach (var variableType in variableTypes.Distinct())
        {
            yield return typeDescriber.DescribeType(variableType);
        }
    }
}