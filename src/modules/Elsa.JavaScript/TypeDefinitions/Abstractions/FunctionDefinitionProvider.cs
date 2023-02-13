using Elsa.JavaScript.TypeDefinitions.Builders;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.TypeDefinitions.Abstractions;

/// <summary>
/// A base class for function definition providers.
/// </summary>
public abstract class FunctionDefinitionProvider : IFunctionDefinitionProvider
{
    /// <summary>
    /// Returns a list of type definitions to the system.
    /// </summary>
    protected virtual ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        var functions = GetFunctionDefinitions(context);
        return new(functions);
    }

    /// <summary>
    /// Returns a list of type definitions to the system.
    /// </summary>
    protected virtual IEnumerable<FunctionDefinition> GetFunctionDefinitions(TypeDefinitionContext context)
    {
        yield break;
    }

    async ValueTask<IEnumerable<FunctionDefinition>> IFunctionDefinitionProvider.GetFunctionDefinitionsAsync(TypeDefinitionContext context) => await GetFunctionDefinitionsAsync(context);

    /// <summary>
    /// Provides a fluid API to build a function definition.
    /// </summary>
    protected FunctionDefinition CreateFunctionDefinition(Action<FunctionDefinitionBuilder> setup)
    {
        var builder = new FunctionDefinitionBuilder();
        setup(builder);
        return builder.BuildFunctionDefinition();
    }
}