using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.MongoDb.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Elsa.ModularPersistence.MongoDb.Services;

/// <summary>
/// Materializes declared modular persistence indexes into MongoDB.
/// </summary>
public sealed class MongoDbDocumentSchemaMaterializer(MongoDbModularPersistenceOptions options) : IStorageManifestMaterializer
{
    public const string ProviderNameValue = "MongoDB";

    private readonly MongoDbCollectionResolver _collectionResolver = new(options);

    public string ProviderName => ProviderNameValue;

    public bool CanMaterialize(StorageManifestDescriptor manifest) => true;

    public async ValueTask MaterializeAsync(StorageManifestDescriptor manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var database = CreateDatabase();
        foreach (var group in GroupStorageUnitsByCollection(manifest))
        {
            var collection = database.GetCollection<BsonDocument>(group.CollectionName);
            var indexes = BuildIndexModels(group.StorageUnits, options.CollectionStrategy).ToArray();

            if (indexes.Length > 0)
                await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
        }
    }

    private IMongoDatabase CreateDatabase()
    {
        var client = new MongoClient(options.ConnectionString);
        return client.GetDatabase(options.DatabaseName);
    }

    private IEnumerable<(string CollectionName, IReadOnlyCollection<StorageUnitDescriptor> StorageUnits)> GroupStorageUnitsByCollection(StorageManifestDescriptor manifest)
    {
        return manifest.StorageUnits
            .GroupBy(x => _collectionResolver.GetCollectionName(x.Name), StringComparer.Ordinal)
            .Select(x => (x.Key, (IReadOnlyCollection<StorageUnitDescriptor>)x.ToArray()));
    }

    private static IEnumerable<CreateIndexModel<BsonDocument>> BuildIndexModels(
        IReadOnlyCollection<StorageUnitDescriptor> storageUnits,
        MongoDbCollectionStrategy collectionStrategy)
    {
        yield return new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys
                .Ascending("Type")
                .Ascending("TenantId")
                .Ascending("Id"),
            new CreateIndexOptions { Name = "IX_ModularPersistenceDocuments_Key" });

        foreach (var storageUnit in storageUnits)
        {
            foreach (var index in storageUnit.Indexes)
            {
                var keys = collectionStrategy == MongoDbCollectionStrategy.SharedCollection
                    ? Builders<BsonDocument>.IndexKeys.Ascending("Type").Ascending("TenantId")
                    : Builders<BsonDocument>.IndexKeys.Ascending("TenantId");

                foreach (var field in index.Fields)
                    keys = field.SortOrder == StorageIndexSortOrder.Descending
                        ? keys.Descending(GetDataFieldPath(field.FieldName))
                        : keys.Ascending(GetDataFieldPath(field.FieldName));

                yield return new CreateIndexModel<BsonDocument>(
                    keys,
                    new CreateIndexOptions
                    {
                        Name = SanitizeIndexName($"IX_ModularPersistenceDocuments_{storageUnit.Name}_{index.Name}"),
                        Unique = index.IsUnique
                    });
            }
        }
    }

    private static string GetDataFieldPath(string fieldName) => $"Data.{fieldName}";

    private static string SanitizeIndexName(string value)
    {
        var chars = value.Select(x => char.IsLetterOrDigit(x) ? x : '_').ToArray();
        return new string(chars);
    }
}
