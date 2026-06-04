namespace Elsa.Persistence.VNext;

public record PersistencePrimaryKey(
    string Name,
    IReadOnlyList<string> Columns);
