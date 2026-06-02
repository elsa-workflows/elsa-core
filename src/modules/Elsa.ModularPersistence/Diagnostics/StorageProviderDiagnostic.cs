namespace Elsa.ModularPersistence.Diagnostics;

public sealed record StorageProviderDiagnostic(
    string ProviderName,
    bool IsSelected,
    bool IsRegistered,
    int MaterializableManifestCount);
