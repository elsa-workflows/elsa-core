namespace Elsa.ModularPersistence.MongoDb.Options;

/// <summary>
/// Configures the MongoDB modular persistence provider.
/// </summary>
public sealed class MongoDbModularPersistenceOptions
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    public string DatabaseName { get; set; } = "ElsaModularPersistence";

    public MongoDbCollectionStrategy CollectionStrategy { get; set; }

    public MongoDbTransactionMode TransactionMode { get; set; }

    public string SharedCollectionName { get; set; } = "ModularPersistenceDocuments";

    public string CollectionPerTypePrefix { get; set; } = "ModularPersistence";
}
