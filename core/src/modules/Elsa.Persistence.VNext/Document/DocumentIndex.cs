namespace Elsa.Persistence.VNext.Document;

public record DocumentIndex(
    string Name,
    string Collection,
    string? Namespace,
    IReadOnlyList<string> Fields,
    bool IsUnique);
