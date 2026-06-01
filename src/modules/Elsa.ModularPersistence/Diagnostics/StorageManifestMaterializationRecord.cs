namespace Elsa.ModularPersistence.Diagnostics;

public sealed record StorageManifestMaterializationRecord(
    string ProviderName,
    string SchemaName,
    string Version,
    DateTimeOffset RecordedAt,
    bool Succeeded,
    string? ErrorType,
    string? ErrorMessage);
