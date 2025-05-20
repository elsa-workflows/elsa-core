using System.Text.Json.Nodes;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.Providers;

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
