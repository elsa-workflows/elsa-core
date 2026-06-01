using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.MongoDb.Options;
using MongoDB.Driver;

namespace Elsa.ModularPersistence.MongoDb.Services;

/// <summary>
/// MongoDB document store for modular persistence.
/// </summary>
public sealed class MongoDbDocumentStore(MongoDbModularPersistenceOptions options, StorageManifestDescriptor manifest) : IDocumentStore
{
    private readonly MongoClient _client = new(options.ConnectionString);
    private readonly MongoDbCollectionResolver _collectionResolver = new(options);

    public ValueTask<IDocumentSession> OpenSessionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IDocumentSession>(new MongoDbDocumentSession(_client, _client.GetDatabase(options.DatabaseName), _collectionResolver, options, manifest));
    }
}
