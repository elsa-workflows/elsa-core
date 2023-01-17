using Elsa.JavaScript.Models;

namespace Elsa.JavaScript.Services;

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