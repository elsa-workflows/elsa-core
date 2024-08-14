using System.Text.Json.Nodes;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.TypeDefinitions.Providers;

/// <summary>
/// Produces <see cref="FunctionDefinition"/>s for common functions.
/// </summary>
internal class CommonTypeDefinitionProvider(ITypeDescriber typeDescriber) : TypeDefinitionProvider
{
    protected override IEnumerable<TypeDefinition> GetTypeDefinitions(TypeDefinitionContext context)
    {
        yield return _typeDescriber.DescribeType(typeof(Guid));
        yield return _typeDescriber.DescribeType(typeof(JsonObject));
        yield return _typeDescriber.DescribeType(typeof(Random));
    }
}