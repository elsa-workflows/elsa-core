namespace Elsa.Persistence.VNext.Relational;

public record RelationalSchemaPlan(
    IReadOnlyList<RelationalTable> Tables,
    IReadOnlyList<RelationalIndex> Indexes);
