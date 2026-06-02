using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Sqlite.Services;

public sealed class SqliteRuntimeEntityDocumentStoreFactory(SqliteModularPersistenceConnectionFactory connectionFactory) : IRuntimeEntityDocumentStoreFactory
{
    public string ProviderName => SqliteDocumentSchemaMaterializer.ProviderNameValue;

    public IDocumentStore CreateStore(StorageManifestDescriptor manifest) => new SqliteDocumentStore(connectionFactory, manifest);
}
