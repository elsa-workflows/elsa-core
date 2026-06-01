using Elsa.ModularPersistence.Queries;

namespace Elsa.ModularPersistence.Documents;

/// <summary>
/// Coordinates document operations in one provider session.
/// </summary>
public interface IDocumentSession : IAsyncDisposable
{
    ValueTask<DocumentEnvelope?> LoadAsync(DocumentKey key, CancellationToken cancellationToken = default);

    ValueTask<DocumentSaveResult> SaveAsync(DocumentEnvelope document, ExpectedDocumentVersion expectedVersion = default, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyCollection<DocumentEnvelope>> QueryAsync(DocumentQuery query, CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(DocumentKey key, ExpectedDocumentVersion expectedVersion = default, CancellationToken cancellationToken = default);
}
