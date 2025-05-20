using Elsa.Scripting.JavaScript.TypeDefinitions.Models;

namespace Elsa.Scripting.JavaScript.TypeDefinitions.Contracts;

/// <summary>
/// Provides <see cref="FunctionDefinition"/>s to the type definition document being constructed.
/// </summary>
public interface IFunctionDefinitionProvider
{
    /// <summary>
    /// Returns a list of type definitions to the system.
    /// </summary>
    ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context);
}