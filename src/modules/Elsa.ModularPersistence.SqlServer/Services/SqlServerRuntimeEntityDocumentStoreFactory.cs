using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.SqlServer.Services;

public sealed class SqlServerRuntimeEntityDocumentStoreFactory(SqlServerModularPersistenceConnectionFactory connectionFactory) : IRuntimeEntityDocumentStoreFactory
{
    public string ProviderName => SqlServerDocumentSchemaMaterializer.ProviderNameValue;

    public IDocumentStore CreateStore(StorageManifestDescriptor manifest) => new SqlServerDocumentStore(connectionFactory, manifest);
}
