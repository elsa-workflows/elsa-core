namespace Elsa.Persistence.VNext;

public record PersistenceColumn(
    string Name,
    PersistenceColumnType Type,
    bool IsNullable = true,
    int? Length = null);
