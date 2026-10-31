namespace Elsa.Persistence.VNext.Relational;

public record RelationalIndex(
    string Name,
    string Table,
    string? Schema,
    IReadOnlyList<string> Columns,
    bool IsUnique);
