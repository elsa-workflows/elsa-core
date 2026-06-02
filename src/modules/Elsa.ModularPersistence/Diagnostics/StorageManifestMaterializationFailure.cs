namespace Elsa.ModularPersistence.Diagnostics;

public sealed record StorageManifestMaterializationFailure(
    string ProviderName,
    string SchemaName,
    string Version,
    DateTimeOffset FailedAt,
    string ErrorType,
    string ErrorMessage);
