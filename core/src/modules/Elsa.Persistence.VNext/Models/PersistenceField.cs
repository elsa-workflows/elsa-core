namespace Elsa.Persistence.VNext;

public record PersistenceField(
    string Name,
    PersistenceColumnType Type,
    bool IsNullable = true,
    int? Length = null);
