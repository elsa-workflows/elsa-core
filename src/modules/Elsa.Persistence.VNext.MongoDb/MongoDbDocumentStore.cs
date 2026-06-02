using Elsa.Persistence.VNext.Document;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Elsa.Persistence.VNext.MongoDb;

public class MongoDbDocumentStore : IDocumentStore
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbDatabasePlan _plan;

    public MongoDbDocumentStore(IMongoDatabase database, PersistenceSchema schema) : this(database, new MongoDbDatabasePlanner().Plan(schema))
    {
    }

    public MongoDbDocumentStore(IMongoDatabase database, MongoDbDatabasePlan plan)
    {
        _database = database;
        _plan = plan;
    }

    public async Task MaterializeAsync(CancellationToken cancellationToken = default)
    {
        foreach (var collectionPlan in _plan.Collections)
        {
            await EnsureCollectionExistsAsync(collectionPlan.CollectionName, cancellationToken);
            var collection = GetMongoCollection(collectionPlan);
            var indexes = collectionPlan.Indexes
                .Select(index => new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Combine(index.Fields.Select(field => Builders<BsonDocument>.IndexKeys.Ascending(field))),
                    new CreateIndexOptions { Name = index.Name, Unique = index.IsUnique }))
                .ToList();

            if (indexes.Count > 0)
                await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
        }
    }

    public async Task<StoredDocument> SaveAsync(SaveDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var collectionPlan = GetCollectionPlan(request.StorageUnit);
        ValidateSaveRequest(collectionPlan.Collection, request);
        var collection = GetMongoCollection(collectionPlan);
        var existing = await LoadDocumentAsync(collection, request.StorageUnit, request.Id, cancellationToken);
        ValidateExpectedVersion(request.StorageUnit, request.Id, request.ExpectedVersion, existing?.Version);

        var now = DateTimeOffset.UtcNow;
        var createdAt = existing?.CreatedAt ?? now;
        var version = request.ExpectedVersion is null ? existing?.Version + 1 ?? 1 : request.ExpectedVersion.Value + 1;
        var saved = new StoredDocument(request.StorageUnit, request.Id, request.Content, version, createdAt, now);
        var document = CreateDocument(saved, request.IndexValues);

        if (request.ExpectedVersion == 0)
        {
            try
            {
                await collection.InsertOneAsync(document, cancellationToken: cancellationToken);
            }
            catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                await ThrowConcurrencyExceptionAsync(collection, request.StorageUnit, request.Id, request.ExpectedVersion.Value, cancellationToken);
            }
            catch (MongoDuplicateKeyException)
            {
                await ThrowConcurrencyExceptionAsync(collection, request.StorageUnit, request.Id, request.ExpectedVersion.Value, cancellationToken);
            }

            return saved;
        }

        var result = await collection.ReplaceOneAsync(
            CreateSaveFilter(request),
            document,
            new ReplaceOptions { IsUpsert = request.ExpectedVersion is null },
            cancellationToken);

        if (request.ExpectedVersion is not null && !DidExpectedWriteSucceed(result))
            await ThrowConcurrencyExceptionAsync(collection, request.StorageUnit, request.Id, request.ExpectedVersion.Value, cancellationToken);

        return saved;
    }

    public async Task<StoredDocument?> LoadAsync(string storageUnit, string id, CancellationToken cancellationToken = default)
    {
        var collectionPlan = GetCollectionPlan(storageUnit);
        return await LoadDocumentAsync(GetMongoCollection(collectionPlan), storageUnit, id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string storageUnit, string id, long? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var collectionPlan = GetCollectionPlan(storageUnit);
        var collection = GetMongoCollection(collectionPlan);
        var existing = await LoadDocumentAsync(collection, storageUnit, id, cancellationToken);
        if (existing is null)
        {
            ValidateExpectedVersion(storageUnit, id, expectedVersion, null);
            return false;
        }

        ValidateExpectedVersion(storageUnit, id, expectedVersion, existing.Version);
        var result = await collection.DeleteOneAsync(CreateDeleteFilter(id, expectedVersion), cancellationToken);
        if (expectedVersion is not null && result.DeletedCount == 0)
            await ThrowConcurrencyExceptionAsync(collection, storageUnit, id, expectedVersion.Value, cancellationToken);

        return result.DeletedCount > 0;
    }

    public async Task<IReadOnlyList<StoredDocument>> QueryAsync(DocumentQuery query, CancellationToken cancellationToken = default)
    {
        var collectionPlan = GetCollectionPlan(query.StorageUnit);
        _ = DocumentIndexMatcher.FindMatchingIndex(collectionPlan.Collection, query);
        var filters = query.Filters
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(filter => Builders<BsonDocument>.Filter.Eq($"IndexValues.{filter.Key}", filter.Value is null ? BsonNull.Value : BsonValue.Create(filter.Value)))
            .ToList();
        var mongoFilter = Builders<BsonDocument>.Filter.And(filters);
        var documents = await GetMongoCollection(collectionPlan)
            .Find(mongoFilter)
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .ToListAsync(cancellationToken);

        return documents.Select(document => ReadDocument(document, query.StorageUnit)).ToList();
    }

    private MongoDbCollectionPlan GetCollectionPlan(string storageUnit)
    {
        return _plan.Collections.SingleOrDefault(x => string.Equals(x.Collection.Name, storageUnit, StringComparison.Ordinal))
            ?? throw new DocumentStoreValidationException($"Storage unit '{storageUnit}' is not declared in the persistence schema.");
    }

    private IMongoCollection<BsonDocument> GetMongoCollection(MongoDbCollectionPlan collectionPlan)
    {
        return _database.GetCollection<BsonDocument>(collectionPlan.CollectionName);
    }

    private async Task EnsureCollectionExistsAsync(string collectionName, CancellationToken cancellationToken)
    {
        using var cursor = await _database.ListCollectionNamesAsync(new ListCollectionNamesOptions
        {
            Filter = new BsonDocument("name", collectionName)
        }, cancellationToken);

        var exists = await cursor.MoveNextAsync(cancellationToken) && cursor.Current.Any();
        if (!exists)
            await _database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken);
    }

    private static void ValidateSaveRequest(DocumentCollection collection, SaveDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
            throw new DocumentStoreValidationException("Document ID is required.");

        var missingFields = collection.Indexes
            .SelectMany(x => x.Fields)
            .Distinct(StringComparer.Ordinal)
            .Where(field => !request.IndexValues.ContainsKey(field))
            .Order(StringComparer.Ordinal)
            .ToList();

        if (missingFields.Count > 0)
            throw new DocumentStoreValidationException($"Storage unit '{collection.Name}' requires index values for fields '{string.Join(", ", missingFields)}'.");
    }

    private static void ValidateExpectedVersion(string storageUnit, string documentId, long? expectedVersion, long? actualVersion)
    {
        if (expectedVersion is null)
            return;

        if (expectedVersion != (actualVersion ?? 0))
            throw new DocumentStoreConcurrencyException(storageUnit, documentId, expectedVersion, actualVersion);
    }

    private static async Task<StoredDocument?> LoadDocumentAsync(IMongoCollection<BsonDocument> collection, string storageUnit, string id, CancellationToken cancellationToken)
    {
        var document = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ReadDocument(document, storageUnit);
    }

    private static FilterDefinition<BsonDocument> CreateSaveFilter(SaveDocumentRequest request)
    {
        var filter = Builders<BsonDocument>.Filter;

        if (request.ExpectedVersion is null)
            return filter.Eq("_id", request.Id);

        return filter.And(filter.Eq("_id", request.Id), filter.Eq("Version", request.ExpectedVersion.Value));
    }

    private static FilterDefinition<BsonDocument> CreateDeleteFilter(string id, long? expectedVersion)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        return expectedVersion is null ? filter : Builders<BsonDocument>.Filter.And(filter, Builders<BsonDocument>.Filter.Eq("Version", expectedVersion.Value));
    }

    private static bool DidExpectedWriteSucceed(ReplaceOneResult result)
    {
        if (!result.IsAcknowledged)
            return true;

        return result.MatchedCount > 0;
    }

    private static async Task ThrowConcurrencyExceptionAsync(IMongoCollection<BsonDocument> collection, string storageUnit, string documentId, long expectedVersion, CancellationToken cancellationToken)
    {
        var actual = await LoadDocumentAsync(collection, storageUnit, documentId, cancellationToken);
        throw new DocumentStoreConcurrencyException(storageUnit, documentId, expectedVersion, actual?.Version);
    }

    private static BsonDocument CreateDocument(StoredDocument document, IReadOnlyDictionary<string, string?> indexValues)
    {
        return new BsonDocument
        {
            ["_id"] = document.Id,
            ["Content"] = document.Content,
            ["Version"] = document.Version,
            ["CreatedAt"] = new BsonDateTime(document.CreatedAt.UtcDateTime),
            ["UpdatedAt"] = new BsonDateTime(document.UpdatedAt.UtcDateTime),
            ["Data"] = ParseContent(document.Content),
            ["IndexValues"] = new BsonDocument(indexValues.Select(x => new BsonElement(x.Key, x.Value is null ? BsonNull.Value : BsonValue.Create(x.Value))))
        };
    }

    private static BsonValue ParseContent(string content)
    {
        return BsonDocument.TryParse(content, out var document) ? document : BsonValue.Create(content);
    }

    private static StoredDocument ReadDocument(BsonDocument document, string storageUnit)
    {
        return new StoredDocument(
            StorageUnit: storageUnit,
            Id: document["_id"].AsString,
            Content: document["Content"].AsString,
            Version: document["Version"].ToInt64(),
            CreatedAt: document["CreatedAt"].ToUniversalTime(),
            UpdatedAt: document["UpdatedAt"].ToUniversalTime());
    }
}
