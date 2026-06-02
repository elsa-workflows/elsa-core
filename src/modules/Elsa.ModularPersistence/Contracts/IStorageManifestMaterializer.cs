using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Contracts;

/// <summary>
/// Materializes storage manifests for a persistence provider.
/// </summary>
public interface IStorageManifestMaterializer
{
    bool CanMaterialize(StorageManifestDescriptor manifest);

    ValueTask MaterializeAsync(StorageManifestDescriptor manifest, CancellationToken cancellationToken = default);
}
