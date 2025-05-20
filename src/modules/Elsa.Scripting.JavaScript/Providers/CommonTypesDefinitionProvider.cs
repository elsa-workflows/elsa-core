using System.Text.Json.Nodes;
using Elsa.Scripting.JavaScript.TypeDefinitions.Abstractions;
using Elsa.Scripting.JavaScript.TypeDefinitions.Contracts;
using Elsa.Scripting.JavaScript.TypeDefinitions.Models;

namespace Elsa.Scripting.JavaScript.Providers;

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
