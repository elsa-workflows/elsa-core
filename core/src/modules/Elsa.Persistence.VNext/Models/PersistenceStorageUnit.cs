namespace Elsa.Persistence.VNext;

public record PersistenceStorageUnit(
    string Name,
    string? Namespace,
    IReadOnlyList<PersistenceField> Fields,
    PersistencePrimaryKey? Key,
    IReadOnlyList<PersistenceIndex> Indexes);
