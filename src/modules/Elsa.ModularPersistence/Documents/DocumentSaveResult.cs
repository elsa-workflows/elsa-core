namespace Elsa.ModularPersistence.Documents;

/// <summary>
/// Describes the result of saving a document.
/// </summary>
public sealed record DocumentSaveResult(DocumentKey Key, long Version);
