using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.MongoDb.Options;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.MongoDb.Services;

public sealed class MongoDbRuntimeEntityDocumentStoreFactory(MongoDbModularPersistenceOptions options) : IRuntimeEntityDocumentStoreFactory
{
    public string ProviderName => MongoDbDocumentSchemaMaterializer.ProviderNameValue;

    public IDocumentStore CreateStore(StorageManifestDescriptor manifest) => new MongoDbDocumentStore(options, manifest);
}
