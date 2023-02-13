using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.TypeDefinitions.Contracts;

/// <summary>
/// Provides <see cref="TypeDefinition"/>s to the type definition document being constructed.
/// </summary>
public interface ITypeDefinitionProvider
{
    /// <summary>
    /// Returns a list of type definitions to the system.
    /// </summary>
    ValueTask<IEnumerable<TypeDefinition>> GetTypeDefinitionsAsync(TypeDefinitionContext context);
}