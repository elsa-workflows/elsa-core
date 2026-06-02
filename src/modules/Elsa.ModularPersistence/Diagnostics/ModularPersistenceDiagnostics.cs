namespace Elsa.ModularPersistence.Diagnostics;

public sealed record ModularPersistenceDiagnostics(
    string? SelectedProviderName,
    bool MaterializeOnStartup,
    IReadOnlyCollection<StorageProviderDiagnostic> Providers,
    IReadOnlyCollection<StorageManifestDiagnostic> Manifests,
    IReadOnlyCollection<StorageManifestMaterializationFailure> MaterializationFailures);
