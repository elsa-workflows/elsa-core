using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Services;

public sealed class StorageManifestRegistry(IEnumerable<StorageManifestRegistration> registrations) : IStorageManifestRegistry
{
    public IReadOnlyCollection<StorageManifestDescriptor> Manifests { get; } = registrations
        .Select(x => x.Manifest)
        .DistinctBy(x => (x.SchemaName, x.Version))
        .ToArray();
}
