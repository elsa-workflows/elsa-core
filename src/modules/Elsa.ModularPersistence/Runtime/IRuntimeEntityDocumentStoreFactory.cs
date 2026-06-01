using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;

namespace Elsa.ModularPersistence.Runtime;

public interface IRuntimeEntityDocumentStoreFactory
{
    string ProviderName { get; }

    IDocumentStore CreateStore(StorageManifestDescriptor manifest);
}
