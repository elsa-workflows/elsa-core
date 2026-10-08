using System.Text.Json.Nodes;
using Elsa.Expressions.JavaScript.TypeDefinitions.Abstractions;
using Elsa.Expressions.JavaScript.TypeDefinitions.Contracts;
using Elsa.Expressions.JavaScript.TypeDefinitions.Models;

namespace Elsa.Expressions.JavaScript.Providers;

/// <summary>
/// Produces <see cref="TypeDefinition"/>s for common types.
/// </summary>
internal class CommonTypeDefinitionProvider(ITypeDescriber typeDescriber) : TypeDefinitionProvider
{
    protected override IEnumerable<TypeDefinition> GetTypeDefinitions(TypeDefinitionContext context)
    {
        yield return typeDescriber.DescribeType(typeof(Guid));
        yield return typeDescriber.DescribeType(typeof(JsonObject));
        yield return typeDescriber.DescribeType(typeof(Random));
    }
}
