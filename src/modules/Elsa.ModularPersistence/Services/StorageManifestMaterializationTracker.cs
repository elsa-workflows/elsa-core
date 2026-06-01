using System.Collections.Concurrent;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Diagnostics;

namespace Elsa.ModularPersistence.Services;

public sealed class StorageManifestMaterializationTracker : IStorageManifestMaterializationTracker
{
    private readonly ConcurrentQueue<StorageManifestMaterializationRecord> _records = new();

    public IReadOnlyCollection<StorageManifestMaterializationRecord> Records => _records.ToArray();

    public void RecordApplied(string providerName, string schemaName, string version, DateTimeOffset appliedAt)
    {
        _records.Enqueue(new StorageManifestMaterializationRecord(providerName, schemaName, version, appliedAt, true, null, null));
    }

    public void RecordFailed(string providerName, string schemaName, string version, DateTimeOffset failedAt, Exception exception)
    {
        _records.Enqueue(new StorageManifestMaterializationRecord(providerName, schemaName, version, failedAt, false, exception.GetType().Name, exception.Message));
    }
}
