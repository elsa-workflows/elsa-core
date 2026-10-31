namespace Elsa.Persistence.VNext;

public record PersistenceTable(
    string Name,
    string? Schema,
    IReadOnlyList<PersistenceColumn> Columns,
    PersistencePrimaryKey? PrimaryKey,
    IReadOnlyList<PersistenceIndex> Indexes);
