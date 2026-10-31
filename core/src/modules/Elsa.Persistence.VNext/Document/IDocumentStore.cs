namespace Elsa.Persistence.VNext.Document;

public interface IDocumentStore
{
    Task MaterializeAsync(CancellationToken cancellationToken = default);

    Task<StoredDocument> SaveAsync(SaveDocumentRequest request, CancellationToken cancellationToken = default);

    Task<StoredDocument?> LoadAsync(string storageUnit, string id, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string storageUnit, string id, long? expectedVersion = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredDocument>> QueryAsync(DocumentQuery query, CancellationToken cancellationToken = default);
}
