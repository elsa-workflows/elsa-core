using Elsa.Scripting.JavaScript.TypeDefinitions.Models;

namespace Elsa.Scripting.JavaScript.TypeDefinitions.Contracts;

/// <summary>
/// Renders a <see cref="TypeDefinitionsDocument"/> to a string.
/// </summary>
public interface ITypeDefinitionDocumentRenderer
{
    /// <summary>
    /// Renders a <see cref="TypeDefinitionsDocument"/> to a string.
    /// </summary>
    string Render(TypeDefinitionsDocument document);
}