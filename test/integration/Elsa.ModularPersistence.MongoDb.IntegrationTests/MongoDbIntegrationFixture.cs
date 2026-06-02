using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.MongoDb.Options;
using Elsa.ModularPersistence.MongoDb.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Elsa.ModularPersistence.MongoDb.IntegrationTests;

public sealed class MongoDbIntegrationFixture : IAsyncDisposable
{
    private const string ConnectionStringEnvironmentVariable = "ELSA_MODULAR_PERSISTENCE_MONGODB_CONNECTION_STRING";
    private readonly string? _connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
    private readonly string _databaseName = $"ElsaModularPersistenceTests_{Guid.NewGuid():N}";

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_connectionString);

    public MongoDbModularPersistenceOptions CreateOptions(MongoDbTransactionMode transactionMode = MongoDbTransactionMode.Disabled) =>
        new()
        {
            ConnectionString = GetConnectionString(),
            DatabaseName = _databaseName,
            TransactionMode = transactionMode
        };

    public MongoDbDocumentStore CreateStore(StorageManifestDescriptor manifest) =>
        new(CreateOptions(), manifest);

    public async ValueTask MaterializeAsync(StorageManifestDescriptor manifest)
    {
        var materializer = new MongoDbDocumentSchemaMaterializer(CreateOptions());
        await materializer.MaterializeAsync(manifest);
    }

    public async ValueTask<IReadOnlyCollection<string>> ReadIndexNamesAsync(string collectionName)
    {
        var database = CreateDatabase();
        var indexes = await database.GetCollection<BsonDocument>(collectionName).Indexes.ListAsync();
        var names = new List<string>();

        await indexes.ForEachAsync(index => names.Add(index["name"].AsString));
        return names;
    }

    public async ValueTask DisposeAsync()
    {
        if (!IsAvailable)
            return;

        await CreateDatabase().Client.DropDatabaseAsync(_databaseName);
    }

    private IMongoDatabase CreateDatabase()
    {
        var client = new MongoClient(GetConnectionString());
        return client.GetDatabase(_databaseName);
    }

    private string GetConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(_connectionString))
            return _connectionString;

        throw new InvalidOperationException($"Set {ConnectionStringEnvironmentVariable} to run MongoDB modular persistence integration tests.");
    }
}
