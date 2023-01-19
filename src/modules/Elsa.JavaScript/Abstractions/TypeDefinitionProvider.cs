using Elsa.JavaScript.Builders;
using Elsa.JavaScript.Models;
using Elsa.JavaScript.Services;

namespace Elsa.JavaScript.Abstractions;

/// <summary>
/// A base class for type definition providers.
/// </summary>
public abstract class TypeDefinitionProvider : ITypeDefinitionProvider
{
    /// <summary>
    /// Returns a list of type definitions to the system.
    /// </summary>
    protected virtual ValueTask<IEnumerable<TypeDefinition>> GetTypeDefinitionsAsync(TypeDefinitionContext context)
    {
        var functions = GetTypeDefinitions(context);
        return new(functions);
    }

    /// <summary>
    /// Returns a list of type definitions to the system.
    /// </summary>
    protected virtual IEnumerable<TypeDefinition> GetTypeDefinitions(TypeDefinitionContext context)
    {
        yield break;
    }

    async ValueTask<IEnumerable<TypeDefinition>> ITypeDefinitionProvider.GetTypeDefinitionsAsync(TypeDefinitionContext context) => await GetTypeDefinitionsAsync(context);

    /// <summary>
    /// Provides a fluid API to build a type definition.
    /// </summary>
    protected TypeDefinition CreateTypeDefinition(Action<TypeDefinitionBuilder> setup)
    {
        var builder = new TypeDefinitionBuilder();
        setup(builder);
        return builder.BuildTypeDefinition();
    }
}