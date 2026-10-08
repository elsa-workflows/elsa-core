namespace Elsa.Persistence.VNext.Document;

public class DocumentStoreConcurrencyException(string storageUnit, string documentId, long? expectedVersion, long? actualVersion) : InvalidOperationException(
    $"Document '{documentId}' in storage unit '{storageUnit}' expected version '{expectedVersion?.ToString() ?? "<none>"}' but actual version was '{actualVersion?.ToString() ?? "<none>"}'.")
{
    public string StorageUnit { get; } = storageUnit;
    public string DocumentId { get; } = documentId;
    public long? ExpectedVersion { get; } = expectedVersion;
    public long? ActualVersion { get; } = actualVersion;
}
