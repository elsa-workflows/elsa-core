using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.TypeDefinitions.Contracts;

/// <summary>
/// Renders a <see cref="TypeDefinitionsDocument"/> to a string.
/// </summary>
public interface ITypeDefinitionDocumentRenderer
{
    /// Renders a <see cref="TypeDefinitionsDocument"/> to a string.
    string Render(TypeDefinitionsDocument document);
}