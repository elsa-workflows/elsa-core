using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;

namespace Elsa.ModularPersistence.Runtime;

public interface IRuntimeEntityDocumentStoreFactoryRegistry
{
    IDocumentStore CreateStore(StorageManifestDescriptor manifest, string? providerName);
}
