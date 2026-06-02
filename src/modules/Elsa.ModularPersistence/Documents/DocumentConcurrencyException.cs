namespace Elsa.ModularPersistence.Documents;

/// <summary>
/// Represents a failed optimistic concurrency check.
/// </summary>
public class DocumentConcurrencyException(DocumentKey key, string message) : Exception(message)
{
    public DocumentKey Key { get; } = key;
}
