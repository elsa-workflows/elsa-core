namespace Elsa.ModularPersistence.Diagnostics;

public sealed record StorageManifestDiagnostic(
    string SchemaName,
    string Version,
    int StorageUnitCount,
    IReadOnlyCollection<StorageManifestMaterializationRecord> MaterializationRecords);
