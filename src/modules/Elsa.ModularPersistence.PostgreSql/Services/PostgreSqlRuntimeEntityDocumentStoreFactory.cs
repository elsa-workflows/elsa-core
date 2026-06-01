using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.PostgreSql.Services;

public sealed class PostgreSqlRuntimeEntityDocumentStoreFactory(PostgreSqlModularPersistenceConnectionFactory connectionFactory) : IRuntimeEntityDocumentStoreFactory
{
    public string ProviderName => PostgreSqlDocumentSchemaMaterializer.ProviderNameValue;

    public IDocumentStore CreateStore(StorageManifestDescriptor manifest) => new PostgreSqlDocumentStore(connectionFactory, manifest);
}
