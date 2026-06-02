using Elsa.ModularPersistence.Diagnostics;

namespace Elsa.ModularPersistence.Contracts;

public interface IStorageManifestMaterializationTracker
{
    IReadOnlyCollection<StorageManifestMaterializationRecord> Records { get; }

    void RecordApplied(string providerName, string schemaName, string version, DateTimeOffset appliedAt);

    void RecordFailed(string providerName, string schemaName, string version, DateTimeOffset failedAt, Exception exception);
}
