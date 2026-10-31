namespace Elsa.Persistence.VNext;

public record PersistenceSchema(
    string Name,
    int Version,
    IReadOnlyList<PersistenceTable> Tables,
    IReadOnlyList<PersistenceStorageUnit> StorageUnits);
