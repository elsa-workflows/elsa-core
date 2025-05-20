using Elsa.Scripting.JavaScript.TypeDefinitions.Models;

namespace Elsa.Scripting.JavaScript.TypeDefinitions.Contracts;

/// <summary>
/// Generates type definitions for the specified context.
/// </summary>
public interface ITypeDefinitionService
{
    /// <summary>
    /// Generates type definitions for the specified context.
    /// </summary>
    Task<string> GenerateTypeDefinitionsAsync(TypeDefinitionContext context);
}