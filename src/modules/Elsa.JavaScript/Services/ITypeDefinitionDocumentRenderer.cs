using Elsa.JavaScript.Models;

namespace Elsa.JavaScript.Services;

/// <summary>
/// Renders a <see cref="TypeDefinitionsDocument"/> to a string.
/// </summary>
public interface ITypeDefinitionDocumentRenderer
{
    /// Renders a <see cref="TypeDefinitionsDocument"/> to a string.
    string Render(TypeDefinitionsDocument document);
}