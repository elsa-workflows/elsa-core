namespace Elsa.Persistence.VNext.Relational;

public record RelationalPrimaryKey(
    string Name,
    IReadOnlyList<string> Columns);
