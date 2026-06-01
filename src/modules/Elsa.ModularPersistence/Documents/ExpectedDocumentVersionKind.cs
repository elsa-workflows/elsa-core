namespace Elsa.ModularPersistence.Documents;

/// <summary>
/// Describes how a write operation should validate the current document version.
/// </summary>
public enum ExpectedDocumentVersionKind
{
    Any = 0,
    New = 1,
    Exact = 2
}
