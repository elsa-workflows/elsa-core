namespace Elsa.Persistence.VNext.Relational;

public record RelationalTable(
    string Name,
    string? Schema,
    IReadOnlyList<RelationalColumn> Columns,
    RelationalPrimaryKey? PrimaryKey);
