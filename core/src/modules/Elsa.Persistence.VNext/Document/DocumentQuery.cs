namespace Elsa.Persistence.VNext.Document;

public record DocumentQuery(
    string StorageUnit,
    IReadOnlyDictionary<string, string?> Filters);
