using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Contracts;

/// <summary>
/// Provides storage manifests declared by Elsa modules.
/// </summary>
public interface IStorageManifestRegistry
{
    IReadOnlyCollection<StorageManifestDescriptor> Manifests { get; }
}
