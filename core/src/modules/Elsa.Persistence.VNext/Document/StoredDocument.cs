namespace Elsa.Persistence.VNext.Document;

public record StoredDocument(
    string StorageUnit,
    string Id,
    string Content,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
