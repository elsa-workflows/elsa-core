namespace Elsa.Persistence.VNext.Relational;

public record RelationalColumn(
    string Name,
    string StoreType,
    bool IsNullable);
