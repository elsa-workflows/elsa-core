using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;

namespace Elsa.ModularPersistence.Relational.IntegrationTests.Contracts;

public interface IRelationalProviderFixture : IAsyncDisposable
{
    string ProviderName { get; }

    bool IsAvailable { get; }

    ValueTask ResetAsync();

    ValueTask MaterializeAsync(StorageManifestDescriptor manifest);

    IDocumentStore CreateStore(StorageManifestDescriptor manifest);

    ValueTask<bool> TableExistsAsync(string tableName);

    ValueTask<bool> IndexExistsAsync(string indexName);

    ValueTask<int> CountIndexRowsAsync(DocumentKey key);

    ValueTask<IReadOnlyCollection<(string SchemaName, string Version)>> ReadSchemaHistoryAsync();
}
