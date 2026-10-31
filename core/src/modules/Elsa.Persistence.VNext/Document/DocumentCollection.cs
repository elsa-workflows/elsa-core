namespace Elsa.Persistence.VNext.Document;

public record DocumentCollection(
    string Name,
    string? Namespace,
    IReadOnlyList<DocumentField> Fields,
    IReadOnlyList<string> KeyFields,
    IReadOnlyList<DocumentIndex> Indexes);
