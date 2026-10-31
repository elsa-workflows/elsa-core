namespace Elsa.Persistence.VNext;

public record PersistenceIndex(
    string Name,
    IReadOnlyList<string> Columns,
    bool IsUnique = false);
