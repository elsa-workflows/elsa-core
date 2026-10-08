namespace Elsa.Persistence.VNext.Document;

public record SaveDocumentRequest(
    string StorageUnit,
    string Id,
    string Content,
    IReadOnlyDictionary<string, string?> IndexValues,
    long? ExpectedVersion = null);
