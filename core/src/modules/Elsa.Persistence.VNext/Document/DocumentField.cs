namespace Elsa.Persistence.VNext.Document;

public record DocumentField(
    string Name,
    PersistenceColumnType Type,
    bool IsNullable);
